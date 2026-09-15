using System.Collections.Generic;
using UnityEngine;

namespace Neon
{
    /// <summary>
    /// Attach to any block / platform (a GameObject with a square SpriteRenderer and a
    /// BoxCollider2D or an axis-aligned PolygonCollider2D used as a platform collider).
    ///
    /// The block declares:
    ///   - <see cref="outlineColor"/> : the colour of the neon line traced around the surface.
    ///
    /// Blocks may be stretched through x / y scaling. A block therefore covers one or more
    /// unit cells on a shared grid. The <see cref="NeonOutlineManager"/> reads those cells to
    /// build a single continuous neon outline around each connected group. Two blocks whose
    /// <see cref="outlineColor"/> match and that are adjacent are treated as one big block:
    /// their shared internal edges are never drawn, so only the perimeter of the merged shape
    /// is outlined.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class NeonBlock : MonoBehaviour
    {
        [Tooltip("Colour of the neon line traced around the exposed surface of this block.")]
        [SerializeField] private Color outlineColor = new Color(1f, 0.486f, 1f, 1f);

        private NeonOutlineManager manager;

        public Color OutlineColor => outlineColor;

        private SpriteRenderer spriteRenderer;

#if UNITY_EDITOR
        // Prefab Assets (viewed in the Project window, thumbnail generation, etc.) aren't part
        // of any loaded scene. Their component callbacks still fire, but the manager they'd
        // register with must create/parent scene GameObjects, which throws when attempted
        // against a Prefab Asset. Scene instances and prefabs open in Prefab Mode (which has
        // its own valid scene) are unaffected by this check.
        private bool IsPrefabAsset => UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
#endif

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        #if UNITY_EDITOR
            if (IsPrefabAsset) return;
        #endif
            Register();
        }

        private void OnDisable()
        {
            if (manager != null)
                manager.Unregister(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (IsPrefabAsset) return;
            // Defer so the whole selection / undo settles before rebuilding.
            UnityEditor.EditorApplication.delayCall += DeferredRebuild;
        }

        private void DeferredRebuild()
        {
            if (this == null) return;
            if (IsPrefabAsset) return;
            if (manager == null) manager = NeonOutlineManager.FindOrCreate();
            if (manager != null)
            {
                manager.Register(this);
                manager.RequestRebuild();
            }
        }
#endif

        private void Register()
        {
            if (manager == null)
                manager = NeonOutlineManager.FindOrCreate();
            if (manager != null)
            {
                manager.Register(this);
                manager.RequestRebuild();
            }
        }

        /// <summary>
        /// World-space, axis-aligned bounds of this block's collider footprint.
        /// </summary>
        public bool TryGetWorldBounds(out Bounds bounds)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                bounds = col.bounds;
                return true;
            }

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                bounds = spriteRenderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        /// <summary>
        /// Rasterises this block onto the shared grid, returning every integer cell it covers.
        /// A block stretched to (3,1) scale on a unit grid returns three horizontally adjacent
        /// cells, allowing the manager to treat stretched blocks as a run of unit cells.
        /// </summary>
        public void CollectCells(float grid, List<Vector2Int> into)
        {
            if (!TryGetWorldBounds(out Bounds b)) return;

            // Convert to cell coordinates. We use the min corner as the cell anchor and round
            // to be robust against tiny float error from scaling / collider bounds.
            int minX = Mathf.RoundToInt(b.min.x / grid);
            int minY = Mathf.RoundToInt(b.min.y / grid);
            int maxX = Mathf.RoundToInt(b.max.x / grid);
            int maxY = Mathf.RoundToInt(b.max.y / grid);

            // At least one cell.
            if (maxX <= minX) maxX = minX + 1;
            if (maxY <= minY) maxY = minY + 1;

            for (int x = minX; x < maxX; x++)
                for (int y = minY; y < maxY; y++)
                    into.Add(new Vector2Int(x, y));
        }
    }
}