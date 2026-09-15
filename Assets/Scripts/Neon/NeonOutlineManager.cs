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
    ///   4. Each loop is drawn with a LineRenderer using the group's outline colour, parented
    ///      to whichever block(s) contributed its edges so it rides along automatically when
    ///      that block moves (see <see cref="ChooseParent"/>).
    /// </summary>
    [ExecuteAlways]
    public class NeonOutlineManager : MonoBehaviour
    {
        [Tooltip("Size (world units) of one grid cell. Must match the NeonBlock cell size.")]
        private float cellSize = .1f;

        [Tooltip("Width of the neon line in world units.")]
        private float lineWidth = 0.3f;

        [Tooltip("Material used for the neon LineRenderers. If empty a default additive-ish " +
                 "sprite material is used so the line colour comes through.")]
        [SerializeField] private Material lineMaterial;

        [Tooltip("Sorting layer for the neon lines.")]
        [SerializeField] private string sortingLayer = "Glow";

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
#if UNITY_EDITOR
            // Prefab Assets (viewed in the Project window, thumbnail generation, etc.) aren't
            // part of any loaded scene. Component callbacks still fire on them, but creating or
            // reparenting GameObjects inside a Prefab Asset throws ("residing in a Prefab
            // Asset..."), so skip entirely here. Scene instances, and prefabs open in Prefab
            // Mode (which has its own valid scene), are unaffected by this check.
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                return;
#endif
            ClearLines();
            if (blocks.Count == 0) return;

            float grid = cellSize;

            // Group cells by outline colour. Same-colour cells merge; different colours are
            // independent layers that can outline over one another. We also remember which
            // blocks claimed each cell, so a finished loop can be traced back to the block(s)
            // that produced it and parented accordingly.
            Dictionary<Color, HashSet<Vector2Int>> byColor = new Dictionary<Color, HashSet<Vector2Int>>(new ColorComparer());
            Dictionary<Color, Dictionary<Vector2Int, HashSet<NeonBlock>>> ownersByColor =
                new Dictionary<Color, Dictionary<Vector2Int, HashSet<NeonBlock>>>(new ColorComparer());
            List<Vector2Int> scratch = new List<Vector2Int>();

            foreach (NeonBlock block in blocks)
            {
                if (block == null) continue;
                scratch.Clear();
                block.CollectCells(grid, scratch);
                if (scratch.Count == 0) continue;

                Color key = block.OutlineColor;
                if (!byColor.TryGetValue(key, out HashSet<Vector2Int> set))
                {
                    set = new HashSet<Vector2Int>();
                    byColor[key] = set;
                }
                if (!ownersByColor.TryGetValue(key, out Dictionary<Vector2Int, HashSet<NeonBlock>> ownerMap))
                {
                    ownerMap = new Dictionary<Vector2Int, HashSet<NeonBlock>>();
                    ownersByColor[key] = ownerMap;
                }

                foreach (Vector2Int c in scratch)
                {
                    set.Add(c);
                    if (!ownerMap.TryGetValue(c, out HashSet<NeonBlock> cellOwners))
                    {
                        cellOwners = new HashSet<NeonBlock>();
                        ownerMap[c] = cellOwners;
                    }
                    cellOwners.Add(block);
                }
            }

            foreach (KeyValuePair<Color, HashSet<Vector2Int>> kv in byColor)
                BuildColorGroup(kv.Key, kv.Value, ownersByColor[kv.Key], grid);
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

        private void BuildColorGroup(Color color, HashSet<Vector2Int> cells,
            Dictionary<Vector2Int, HashSet<NeonBlock>> owners, float grid)
        {
            // Collect boundary edges as directed segments so loops wind consistently (CCW,
            // interior on the left). Corner points are integer lattice points (cell corners).
            //
            // A start corner can be shared by two different boundary edges where the shape
            // pinches (two cells touching only at a diagonal), so edges are stored in a
            // multimap: start corner -> list of end corners. edgeOwner remembers which block's
            // cell produced each directed edge, so once a loop is traced we know which
            // block(s) it belongs to.
            Dictionary<Vector2Int, List<Vector2Int>> edges = new Dictionary<Vector2Int, List<Vector2Int>>();
            Dictionary<(Vector2Int, Vector2Int), HashSet<NeonBlock>> edgeOwners =
                new Dictionary<(Vector2Int, Vector2Int), HashSet<NeonBlock>>();

            foreach (Vector2Int cell in cells)
            {
                Vector2Int bl = new Vector2Int(cell.x, cell.y);
                Vector2Int br = new Vector2Int(cell.x + 1, cell.y);
                Vector2Int tr = new Vector2Int(cell.x + 1, cell.y + 1);
                Vector2Int tl = new Vector2Int(cell.x, cell.y + 1);

                owners.TryGetValue(cell, out HashSet<NeonBlock> cellOwners);

                // An edge is drawn only when the neighbouring cell is empty for this colour
                // set. Same-colour neighbours share the cell so the edge is skipped, which is
                // what merges adjacent same-colour blocks into one big outlined shape.
                if (!cells.Contains(cell + new Vector2Int(0, -1)))
                {
                    AddEdge(edges, br, bl); // bottom: right->left
                    edgeOwners[(br, bl)] = cellOwners;
                }

                if (!cells.Contains(cell + new Vector2Int(0, 1)))
                {
                    AddEdge(edges, tl, tr); // top: left->right
                    edgeOwners[(tl, tr)] = cellOwners;
                }
                if (!cells.Contains(cell + new Vector2Int(-1, 0)))
                {
                    AddEdge(edges, bl, tl); // left: bottom->top
                    edgeOwners[(bl, tl)] = cellOwners;
                }
                if (!cells.Contains(cell + new Vector2Int(1, 0)))
                {
                    AddEdge(edges, tr, br); // right: top->bottom
                    edgeOwners[(tr, br)] = cellOwners;
                }
            }

            List<List<Vector2Int>> loops = ExtractLoops(edges);

            foreach (List<Vector2Int> loop in loops)
            {
                // Determine ownership from the RAW loop, before collinear points are removed -
                // every consecutive pair in the raw loop corresponds to exactly one edge we
                // added above, so this is where the edge->block lookup is still valid.
                HashSet<NeonBlock> owningBlocks = CollectLoopOwners(loop, edgeOwners, owners);
                List<Vector2Int> simplified = SimplifyCollinear(loop);
                DrawLoop(simplified, color, grid, owningBlocks);
            }
        }

        private static void AddEdge(Dictionary<Vector2Int, List<Vector2Int>> edges, Vector2Int from, Vector2Int to)
        {
            if (!edges.TryGetValue(from, out List<Vector2Int> list))
            {
                list = new List<Vector2Int>(1);
                edges[from] = list;
            }
            list.Add(to);
        }

        private static HashSet<NeonBlock> CollectLoopOwners(List<Vector2Int> rawLoop,
            Dictionary<(Vector2Int, Vector2Int), HashSet<NeonBlock>> edgeOwners,
            Dictionary<Vector2Int, HashSet<NeonBlock>> owners)
        {
            HashSet<NeonBlock> result = new HashSet<NeonBlock>();
            int n = rawLoop.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2Int from = rawLoop[i];
                Vector2Int to = rawLoop[(i + 1) % n];
                if (edgeOwners.TryGetValue((from, to), out HashSet<NeonBlock> edgeBlockOwners))
                {
                    result.UnionWith(edgeBlockOwners);
                    continue;
                }

                // Recover the cell on the inside of the directed boundary edge if the
                // extracted loop no longer matches the edge dictionary key exactly.
                Vector2Int cell;
                if (from.y == to.y)
                    cell = from.x > to.x
                        ? new Vector2Int(from.x - 1, from.y)
                        : new Vector2Int(from.x, from.y - 1);
                else
                    cell = from.x < to.x
                        ? new Vector2Int(from.x, from.y)
                        : new Vector2Int(from.x - 1, from.y - 1);

                if (owners.TryGetValue(cell, out HashSet<NeonBlock> cellOwners))
                    result.UnionWith(cellOwners);
            }
            return result;
        }

        private static List<List<Vector2Int>> ExtractLoops(Dictionary<Vector2Int, List<Vector2Int>> edges)
        {
            List<List<Vector2Int>> loops = new List<List<Vector2Int>>();

            int RemainingCount()
            {
                int c = 0;
                foreach (KeyValuePair<Vector2Int, List<Vector2Int>> kv in edges) c += kv.Value.Count;
                return c;
            }

            int total = RemainingCount();
            int outerGuard = total + 4;

            while (RemainingCount() > 0 && outerGuard-- > 0)
            {
                // Find any start corner that still has an outgoing edge.
                Vector2Int start = default;
                bool found = false;
                foreach (KeyValuePair<Vector2Int, List<Vector2Int>> kv in edges)
                {
                    if (kv.Value.Count > 0) { start = kv.Key; found = true; break; }
                }
                if (!found) break;

                List<Vector2Int> loop = new List<Vector2Int> { start };
                Vector2Int cur = start;

                int guard = total + 4;
                while (guard-- > 0)
                {
                    if (!edges.TryGetValue(cur, out List<Vector2Int> nexts) || nexts.Count == 0)
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

            List<Vector2Int> result = new List<Vector2Int>(n);
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

        /// <summary>
        /// Decides which transform a loop's LineRenderer should be parented to so it rides
        /// along with the block(s) that produced it:
        ///   - One contributing block -> parent directly to that block. Moving/rotating the
        ///     block moves the outline with it, no rebuild required.
        ///   - Several contributing blocks (a merged same-colour group) that share a common
        ///     ancestor transform somewhere up their hierarchies -> parent to the NEAREST such
        ///     ancestor. Moving that ancestor moves the whole assembled shape as a rigid unit,
        ///     which is the only case where the merge stays geometrically valid without a
        ///     rebuild. This is found via lowest-common-ancestor search rather than requiring
        ///     the blocks to share the same IMMEDIATE parent - a moving group is often organised
        ///     with each block under its own sub-container (for sorting, per-block effects,
        ///     etc.), all of which still sit under one shared root that actually moves. Requiring
        ///     exact same-parent equality would miss that shared root and wrongly fall back to
        ///     the static case below, which looks fine while the group is stationary but leaves
        ///     the outline behind the moment the group moves.
        ///   - Several contributing blocks with no common ancestor at all -> returns null.
        ///     There's no single transform that can carry the merged shape correctly, so the
        ///     caller falls back to a static, manager-parented line (call RequestRebuild() if
        ///     any of those blocks move).
        /// </summary>
        private static Transform ChooseParent(HashSet<NeonBlock> owningBlocks)
        {
            if (owningBlocks == null || owningBlocks.Count == 0) return null;

            if (owningBlocks.Count == 1)
            {
                foreach (NeonBlock b in owningBlocks)
                    return b != null ? b.transform : null;
            }

            Transform commonAncestor = null;
            bool first = true;
            foreach (NeonBlock b in owningBlocks)
            {
                if (b == null) return null;
                if (first)
                {
                    commonAncestor = b.transform;
                    first = false;
                }
                else
                {
                    commonAncestor = FindLowestCommonAncestor(commonAncestor, b.transform);
                    if (commonAncestor == null) return null;
                }
            }
            return commonAncestor;
        }

        /// <summary>
        /// Walks both transforms' ancestor chains (each transform counts as its own ancestor)
        /// and returns the nearest transform present in both chains, or null if they share none.
        /// </summary>
        private static Transform FindLowestCommonAncestor(Transform a, Transform b)
        {
            HashSet<Transform> ancestorsOfA = new HashSet<Transform>();
            for (Transform t = a; t != null; t = t.parent)
                ancestorsOfA.Add(t);

            for (Transform t = b; t != null; t = t.parent)
                if (ancestorsOfA.Contains(t))
                    return t;

            return null;
        }

        private void DrawLoop(List<Vector2Int> corners, Color color, float grid, HashSet<NeonBlock> owningBlocks)
        {
            if (corners.Count < 2) return;

            Transform parent = ChooseParent(owningBlocks);
#if UNITY_EDITOR
            // Belt-and-suspenders: even though Rebuild() already bails when the manager itself
            // is a Prefab Asset, a chosen block parent could in principle be one too (e.g. a
            // nested prefab reference). Parenting into a Prefab Asset throws, so fall back to
            // the static manager-parented line instead of crashing.
            if (parent != null && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(parent.gameObject))
                parent = null;
#endif

            GameObject go = new GameObject("NeonOutline");
            go.hideFlags = HideFlags.DontSave;

            LineRenderer lr = go.AddComponent<LineRenderer>();
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

            if (parent != null)
            {
                // Parent first. Stretched blocks carry non-uniform scale (e.g. x3 to span
                // three cells) - a LineRenderer inherits that scale for its width as well as
                // its length, so a naive child would render thick/thin unevenly along the
                // shape instead of a constant-width neon line. Counteract it on the line's OWN
                // transform so, from the LineRenderer's point of view, it always sits under an
                // effective scale of (1,1,1) - only the parent's position/rotation come
                // through, which is all we want it to inherit.
                go.transform.SetParent(parent, false);
                Vector3 parentScale = parent.lossyScale;
                go.transform.localScale = new Vector3(
                    Mathf.Approximately(parentScale.x, 0f) ? 1f : 1f / parentScale.x,
                    Mathf.Approximately(parentScale.y, 0f) ? 1f : 1f / parentScale.y,
                    Mathf.Approximately(parentScale.z, 0f) ? 1f : 1f / parentScale.z);

                // Convert into the LINE's own local space (not the parent's) now that its
                // transform includes the compensating scale - that keeps the round trip to
                // world space correct regardless of what that compensation is.
                lr.useWorldSpace = false;
                for (int i = 0; i < corners.Count; i++)
                {
                    Vector3 world = new Vector3(corners[i].x * grid, corners[i].y * grid, zOffset);
                    lr.SetPosition(i, go.transform.InverseTransformPoint(world));
                }
            }
            else
            {
                // No single transform can own this shape (disconnected merge) - fall back to
                // the old static, world-space behaviour parented under the manager.
                go.transform.SetParent(transform, false);
                lr.useWorldSpace = true;
                for (int i = 0; i < corners.Count; i++)
                {
                    Vector3 p = new Vector3(corners[i].x * grid, corners[i].y * grid, zOffset);
                    lr.SetPosition(i, p);
                }
            }

            lineObjects.Add(go);
        }

        private static Material cachedDefault;
        private static Material DefaultLineMaterial()
        {
            if (cachedDefault != null) return cachedDefault;
            Shader shader = Shader.Find("Neon/GlowLine");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // fallback
            cachedDefault = new Material(shader) { name = "NeonLine (Runtime)" };
            return cachedDefault;
        }

        private void ClearLines()
        {
            foreach (GameObject go in lineObjects)
            {
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            lineObjects.Clear();

            // Also clear stragglers from earlier rebuilds. Lines can be parented to a shared
            // ancestor, so searching only direct children leaves stale outlines behind.
            void SweepStragglers(Transform root)
            {
                if (root == null) return;
                for (int i = root.childCount - 1; i >= 0; i--)
                {
                    Transform child = root.GetChild(i);
                    if (child.name == "NeonOutline")
                    {
                        if (Application.isPlaying) Destroy(child.gameObject);
                        else DestroyImmediate(child.gameObject);
                    }
                    else
                    {
                        SweepStragglers(child);
                    }
                }
            }

            SweepStragglers(transform.root);
        }

        public static NeonOutlineManager FindOrCreate()
        {
#if UNITY_2023_1_OR_NEWER
            NeonOutlineManager existing = Object.FindFirstObjectByType<NeonOutlineManager>();
#else
            NeonOutlineManager existing = Object.FindObjectOfType<NeonOutlineManager>();
#endif
            if (existing != null) return existing;

            GameObject go = new GameObject("Neon Outline Manager");
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