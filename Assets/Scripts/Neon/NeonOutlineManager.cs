using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    /// <summary>
    /// Builds and renders a single continuous neon outline around groups of adjacent
    /// <see cref="NeonBlock"/>s.
    ///
    /// Algorithm overview:
    ///   1. Every registered block rasterises its collider footprint onto a shared integer grid.
    ///      A stretched block simply fills a run of cells. Each filled cell remembers the
    ///      outline colour of the block that filled it.
    ///   2. For every filled cell we look at its four neighbours. A cell edge is part of the
    ///      neon outline only if the neighbouring cell is NOT filled with the SAME outline
    ///      colour. This is what makes same-colour adjacent blocks act as one big block:
    ///      their shared internal edges have a same-colour neighbour and are skipped, leaving
    ///      only the perimeter of the merged shape. (For the T example the middle of the top
    ///      block's bottom edge and the whole of the bottom block's top edge are shared, so
    ///      only the silhouette of the T is drawn with no lines inside.)
    ///   3. Boundary edges are stitched into closed loops per colour and collinear runs are
    ///      merged, producing continuous lines. A corner vertex is kept only when the two edges
    ///      meeting there are both part of the outline (i.e. the direction actually turns);
    ///      collinear pass-through points are removed.
    ///   4. Each loop is drawn with a LineRenderer using the group's outline colour.
    /// </summary>
    [ExecuteAlways]
    public class NeonOutlineManager : MonoBehaviour
    {
        [Tooltip("Size (world units) of one grid cell. Must match the NeonBlock cell size.")]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Width of the neon line in world units.")]
        [SerializeField] private float lineWidth = 0.08f;

        [Tooltip("Material used for the neon LineRenderers. If empty a default additive-ish " +
                 "sprite material is used so the line colour comes through.")]
        [SerializeField] private Material lineMaterial;

        [Tooltip("Sorting layer for the neon lines.")]
        [SerializeField] private string sortingLayer = "Platforms";

        [Tooltip("Sorting order for the neon lines (drawn above the block fill).")]
        [SerializeField] private int sortingOrder = 10;

        [Tooltip("Z position offset for the generated line objects.")]
        [SerializeField] private float zOffset = -0.1f;

        private readonly HashSet<NeonBlock> blocks = new HashSet<NeonBlock>();
        private readonly List<GameObject> lineObjects = new List<GameObject>();
        private bool rebuildQueued;

        public float CellSize => cellSize;

        public void Register(NeonBlock block)
        {
            if (block != null) blocks.Add(block);
        }

        public void Unregister(NeonBlock block)
        {
            if (block != null && blocks.Remove(block))
                RequestRebuild();
        }

        public void RequestRebuild()
        {
            if (rebuildQueued) return;
            rebuildQueued = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    rebuildQueued = false;
                    if (this != null) Rebuild();
                };
                return;
            }
#endif
        }

        private void OnEnable()
        {
            RequestRebuild();
        }

        private void Update()
        {
            if (rebuildQueued && Application.isPlaying)
            {
                rebuildQueued = false;
                Rebuild();
            }
        }

        /// <summary>Rasterise all blocks, trace merged perimeters and (re)draw them.</summary>
        public void Rebuild()
        {
            ClearLines();
            if (blocks.Count == 0) return;

            float grid = cellSize;

            // Group cells by outline colour. Same-colour cells merge; different colours are
            // independent layers that can outline over one another.
            var byColor = new Dictionary<Color, HashSet<Vector2Int>>(new ColorComparer());
            var scratch = new List<Vector2Int>();

            foreach (var block in blocks)
            {
                if (block == null) continue;
                scratch.Clear();
                block.CollectCells(grid, scratch);
                if (scratch.Count == 0) continue;

                Color key = block.OutlineColor;
                if (!byColor.TryGetValue(key, out var set))
                {
                    set = new HashSet<Vector2Int>();
                    byColor[key] = set;
                }
                foreach (var c in scratch) set.Add(c);
            }

            foreach (var kv in byColor)
                BuildColorGroup(kv.Key, kv.Value, grid);
        }

        // Edge directions used to walk a cell perimeter counter-clockwise.
        // For a filled cell, an edge is drawn only when the outward neighbour in that
        // direction is empty (for this colour set).
        private static readonly Vector2Int[] NeighborDir =
        {
            new Vector2Int(1, 0),   // right  -> right edge
            new Vector2Int(0, 1),   // up     -> top edge
            new Vector2Int(-1, 0),  // left   -> left edge
            new Vector2Int(0, -1),  // down   -> bottom edge
        };

        private void BuildColorGroup(Color color, HashSet<Vector2Int> cells, float grid)
        {
            // Collect boundary edges as directed segments so loops wind consistently (CCW,
            // interior on the left). Corner points are integer lattice points (cell corners).
            //
            // A start corner can be shared by two different boundary edges where the shape
            // pinches (two cells touching only at a diagonal), so edges are stored in a
            // multimap: start corner -> list of end corners.
            var edges = new Dictionary<Vector2Int, List<Vector2Int>>();

            foreach (var cell in cells)
            {
                Vector2Int bl = new Vector2Int(cell.x, cell.y);
                Vector2Int br = new Vector2Int(cell.x + 1, cell.y);
                Vector2Int tr = new Vector2Int(cell.x + 1, cell.y + 1);
                Vector2Int tl = new Vector2Int(cell.x, cell.y + 1);

                // An edge is drawn only when the neighbouring cell is empty for this colour
                // set. Same-colour neighbours share the cell so the edge is skipped, which is
                // what merges adjacent same-colour blocks into one big outlined shape.
                if (!cells.Contains(cell + new Vector2Int(0, -1))) AddEdge(edges, br, bl); // bottom: right->left
                if (!cells.Contains(cell + new Vector2Int(0, 1)))  AddEdge(edges, tl, tr); // top: left->right
                if (!cells.Contains(cell + new Vector2Int(-1, 0))) AddEdge(edges, bl, tl); // left: bottom->top
                if (!cells.Contains(cell + new Vector2Int(1, 0)))  AddEdge(edges, tr, br); // right: top->bottom
            }

            var loops = ExtractLoops(edges);

            foreach (var loop in loops)
            {
                var simplified = SimplifyCollinear(loop);
                DrawLoop(simplified, color, grid);
            }
        }

        private static void AddEdge(Dictionary<Vector2Int, List<Vector2Int>> edges, Vector2Int from, Vector2Int to)
        {
            if (!edges.TryGetValue(from, out var list))
            {
                list = new List<Vector2Int>(1);
                edges[from] = list;
            }
            list.Add(to);
        }

        private static List<List<Vector2Int>> ExtractLoops(Dictionary<Vector2Int, List<Vector2Int>> edges)
        {
            var loops = new List<List<Vector2Int>>();

            int RemainingCount()
            {
                int c = 0;
                foreach (var kv in edges) c += kv.Value.Count;
                return c;
            }

            int total = RemainingCount();
            int outerGuard = total + 4;

            while (RemainingCount() > 0 && outerGuard-- > 0)
            {
                // Find any start corner that still has an outgoing edge.
                Vector2Int start = default;
                bool found = false;
                foreach (var kv in edges)
                {
                    if (kv.Value.Count > 0) { start = kv.Key; found = true; break; }
                }
                if (!found) break;

                var loop = new List<Vector2Int> { start };
                Vector2Int cur = start;

                int guard = total + 4;
                while (guard-- > 0)
                {
                    if (!edges.TryGetValue(cur, out var nexts) || nexts.Count == 0)
                        break;

                    // Consume one outgoing edge from the current corner.
                    Vector2Int next = nexts[nexts.Count - 1];
                    nexts.RemoveAt(nexts.Count - 1);

                    if (next == start) break;
                    loop.Add(next);
                    cur = next;
                }

                if (loop.Count >= 4)
                    loops.Add(loop);
            }

            return loops;
        }

        /// <summary>
        /// Removes points that lie on a straight run so only genuine corners remain. A corner
        /// is kept exactly when the incoming and outgoing edges point in different directions,
        /// i.e. both sides of the corner carry the neon line.
        /// </summary>
        private static List<Vector2Int> SimplifyCollinear(List<Vector2Int> loop)
        {
            int n = loop.Count;
            if (n < 3) return loop;

            var result = new List<Vector2Int>(n);
            for (int i = 0; i < n; i++)
            {
                Vector2Int prev = loop[(i - 1 + n) % n];
                Vector2Int cur = loop[i];
                Vector2Int next = loop[(i + 1) % n];

                Vector2Int inDir = cur - prev;
                Vector2Int outDir = next - cur;

                // Keep only when the direction changes (a real corner where both edges exist).
                if (inDir != outDir)
                    result.Add(cur);
            }
            return result;
        }

        private void DrawLoop(List<Vector2Int> corners, Color color, float grid)
        {
            if (corners.Count < 2) return;

            var go = new GameObject("NeonOutline");
            go.transform.SetParent(transform, false);
            go.hideFlags = HideFlags.DontSave;

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.alignment = LineAlignment.TransformZ;
            lr.textureMode = LineTextureMode.Stretch;
            lr.widthMultiplier = lineWidth;
            lr.material = lineMaterial != null ? lineMaterial : DefaultLineMaterial();

            lr.startColor = color;
            lr.endColor = color;

            lr.sortingLayerName = sortingLayer;
            lr.sortingOrder = sortingOrder;

            lr.positionCount = corners.Count;
            for (int i = 0; i < corners.Count; i++)
            {
                Vector3 p = new Vector3(corners[i].x * grid, corners[i].y * grid, zOffset);
                lr.SetPosition(i, p);
            }

            lineObjects.Add(go);
        }

        private static Material cachedDefault;
        private static Material DefaultLineMaterial()
        {
            if (cachedDefault != null) return cachedDefault;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            cachedDefault = new Material(shader) { name = "NeonLine (Runtime)" };
            return cachedDefault;
        }

        private void ClearLines()
        {
            foreach (var go in lineObjects)
            {
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            lineObjects.Clear();

            // Also clear any stragglers (e.g. after a domain reload) parented under us.
            var stragglers = new List<Transform>();
            foreach (Transform child in transform)
                if (child.name == "NeonOutline")
                    stragglers.Add(child);
            foreach (var t in stragglers)
            {
                if (Application.isPlaying) Destroy(t.gameObject);
                else DestroyImmediate(t.gameObject);
            }
        }

        public static NeonOutlineManager FindOrCreate()
        {
#if UNITY_2023_1_OR_NEWER
            var existing = Object.FindFirstObjectByType<NeonOutlineManager>();
#else
            var existing = Object.FindObjectOfType<NeonOutlineManager>();
#endif
            if (existing != null) return existing;

            var go = new GameObject("Neon Outline Manager");
            return go.AddComponent<NeonOutlineManager>();
        }

        /// <summary>Compares colours with a tolerance so tiny float differences still merge.</summary>
        private class ColorComparer : IEqualityComparer<Color>
        {
            private const float Q = 256f;
            private static int Quant(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * Q);

            public bool Equals(Color a, Color b) =>
                Quant(a.r) == Quant(b.r) && Quant(a.g) == Quant(b.g) &&
                Quant(a.b) == Quant(b.b) && Quant(a.a) == Quant(b.a);

            public int GetHashCode(Color c) =>
                (Quant(c.r) * 397 ^ Quant(c.g)) * 397 ^ Quant(c.b) * 397 ^ Quant(c.a);
        }
    }
}
