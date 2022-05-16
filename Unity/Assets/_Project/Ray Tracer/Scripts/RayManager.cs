using System.Collections.Generic;
using _Project.Ray_Tracer.Scripts.RT_Ray;
using _Project.Ray_Tracer.Scripts.Utility;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;

namespace _Project.Ray_Tracer.Scripts
{
    /// <summary>
    /// Manages the visible rays in the Unity scene. Gets new rays from the ray tracer each frame and draws them.
    /// </summary>
    public class RayManager : MonoBehaviour
    {
        [Header("Render Settings")]

        [SerializeField, Range(0.00f, 1.00f)]
        private float rayHideThreshold = 0.02f;
        /// <summary>
        /// The draw threshold of the rays this ray manager draws.
        /// </summary>
        public float RayHideThreshold
        {
            get { return rayHideThreshold; }
            set { rayHideThreshold = value; }
        }

        [SerializeField]
        private bool rayTransparencyEnabled = true;
        /// <summary>
        /// Whether this ray manager makes rays transparent when it draws.
        /// </summary>
        public bool RayTransparencyEnabled
        {
            get { return rayTransparencyEnabled; }
            set { rayTransparencyEnabled = value; }
        }

        [SerializeField, Range(0.00f, 2.00f)]
        public static float rayTransExponent = 1.00f;
        /// <summary>
        /// The transparency threshold of the rays this ray manager draws.
        /// </summary>
        public float RayTransExponent
        {
            get { return rayTransExponent; }
            set { rayTransExponent = value; }
        }

        [SerializeField, Range(0.00f, 1.00f)]
        private float rayTransThreshold = 0.25f;
        /// <summary>
        /// The transparency threshold of the rays this ray manager draws.
        /// </summary>
        public float RayTransThreshold
        {
            get { return rayTransThreshold; }
            set { rayTransThreshold = value; }
        }

        [SerializeField]
        private bool rayDynamicRadiusEnabled = true;
        /// <summary>
        /// Whether this ray manager's rays' radius is contribution-based.
        /// </summary>
        public bool RayDynamicRadiusEnabled
        {
            get { return rayDynamicRadiusEnabled; }
            set { rayDynamicRadiusEnabled = value; }
        }

        [SerializeField]
        private bool rayColorContributionEnabled = true;
        /// <summary>
        /// Whether this ray manager's rays' color is contribution-based.
        /// </summary>
        public bool RayColorContributionEnabled
        {
            get { return rayColorContributionEnabled; }
            set { rayColorContributionEnabled = value; }
        }

        [SerializeField, Range(0.001f, 0.1f)]
        private float rayRadius = 0.01f;
        /// <summary>
        /// The radius of the rays this ray manager draws.
        /// </summary>
        public float RayRadius
        {
            get { return rayRadius; }
            set { rayRadius = value; }
        }

        [SerializeField, Range(0.001f, 0.1f)]
        private float rayMinRadius = 0.003f;
        /// <summary>
        /// The minimum radius of the rays this ray manager draws.
        /// </summary>
        public float RayMinRadius
        {
            get { return rayMinRadius; }
            set { rayMinRadius = value; }
        }

        [SerializeField, Range(0.001f, 0.1f)]
        private float rayMaxRadius = 0.025f;
        /// <summary>
        /// The maximum radius of the rays this ray manager draws.
        /// </summary>
        public float RayMaxRadius
        {
            get { return rayMaxRadius; }
            set { rayMaxRadius = value; }
        }

        [SerializeField, Range(0.0f, 25.0f)]
        private float infiniteRayDrawLength = 10.0f;
        /// <summary>
        /// The length with which this ray manager draws rays that do not intersect an object.
        /// </summary>
        public float InfiniteRayDrawLength
        {
            get { return infiniteRayDrawLength; }
            set { infiniteRayDrawLength = value; }
        }

        [SerializeField]
        private RayObject rayPrefab;
        [Range(0, 1024)]
        private int initialRayPoolSize = 64;

        /// <summary>
        /// Whether this ray manager hides rays that do not intersect an object.
        /// </summary>
        public bool HideNoHitRays;
        
        /// <summary>
        /// Whether this ray manager hides all rays it would normally draw. When this is <c>false</c>, all ray drawing
        /// and animation code will be skipped.
        /// </summary>
        public bool ShowRays;


        /// <summary>
        /// Whether this ray manager hides negligible rays it would normally draw. When this is <c>false</c>, no ray
        /// will be hidden for contributing too little.
        /// </summary>
        public bool HideNegligibleRays;

        public const int transparencyRange = 50;
        [SerializeField] private Material noHitMaterial;
        [SerializeField] private Material reflectMaterial;
        [SerializeField] private Material reflectMaterialTransparent;
        private Material[] reflectMaterialTransparentArray = new Material[transparencyRange];
        [SerializeField] private Material refractMaterial;
        [SerializeField] private Material refractMaterialTransparent;
        private Material[] refractMaterialTransparentArray = new Material[transparencyRange];
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material normalMaterialTransparent;
        private Material[] normalMaterialTransparentArray = new Material[transparencyRange];
        [SerializeField] private Material shadowMaterial;
        [SerializeField] private Material shadowMaterialTransparent;
        private Material[] shadowMaterialTransparentArray = new Material[transparencyRange];
        [SerializeField] private Material lightMaterial;
        [SerializeField] private Material lightMaterialTransparent;
        private Material[] lightMaterialTransparentArray = new Material[transparencyRange];
        [SerializeField] private Material errorMaterial;

        private const int colorN = 18;
        private const float colorStep = 1f / (colorN - 1);
        [SerializeField] private Material colorRayMaterial;
        [SerializeField] private Material colorRayMaterialTransparent;
        private static Material[,,] colorRayMaterialArray = new Material[colorN, colorN, colorN];
        private static Material[,,,] colorRayMaterialTransparentArray = new Material[colorN, colorN, colorN, transparencyRange];

        [Header("Animation Settings")]

        [SerializeField]
        private bool animate = false;
        /// <summary>
        /// Whether this ray manager animates the rays it draws.
        /// </summary>
        public bool Animate
        {
            get { return animate; }
            set
            {
                Reset = animate != value; // Reset the animation if we changed the value.
                animate = value;
            }
        }

        [SerializeField]
        private bool animateSequentially = false;
        /// <summary>
        /// Whether this ray manager animates the rays sequentially. Does nothing if <see cref="Animate"/> is not set.
        /// </summary>
        public bool AnimateSequentially
        {
            get { return animateSequentially; }
            set
            {
                Reset = animateSequentially != value; // Reset the animation if we changed the value.
                animateSequentially = value;
            }
        }

        [SerializeField]
        private bool loop = false;
        /// <summary>
        /// Whether this ray manager loops its animation. Does nothing if <see cref="Animate"/> is not set.
        /// </summary>
        public bool Loop
        {
            get { return loop; }
            set { loop = value; }
        }

        [SerializeField]
        private bool reset = false;
        /// <summary>
        /// Whether this ray manager will reset its animation on the next frame. After resetting this variable will be
        /// set to <c>false</c> again. Does nothing if <see cref="Animate"/> is not set.
        /// </summary>
        public bool Reset
        {
            get { return reset; }
            set { reset = value; }
        }

        [SerializeField, Range(0.0f, 10.0f)]
        private float speed = 2.0f;
        /// <summary>
        /// The speed of this ray manager's animation. Does nothing if <see cref="Animate"/> is not set.
        /// </summary>
        public float Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        private static RayManager instance = null;

        private List<TreeNode<RTRay>> rays;
        private RayObjectPool rayObjectPool;

        private TreeNode<RTRay> selectedRay;
        private Vector2Int selectedRayCoordinates;
        private bool hasSelectedRay = false;

        private RTSceneManager rtSceneManager;
        private UnityRayTracer rayTracer;
        
        private float distanceToDraw = 0.0f;
        private int rayTreeToDraw = 0; // Used when animating sequentially.
        private bool animationDone = false;

        private bool shouldUpdateRays = true;

        /// <summary>
        /// Get the current <see cref="RayManager"/> instance.
        /// </summary>
        /// <returns> The current <see cref="RayManager"/> instance. </returns>
        public static RayManager Get()
        {
            return instance;
        }

        /// <summary>
        /// Get the colors of the root rays managed by this <see cref="RayManager"/>.
        /// </summary>
        /// <returns> A <see cref="Color"/><c>[]</c> of from rays at the root of their ray tree. </returns>
        public Color[] GetRayColors()
        {
            Color[] colors = new Color[rays.Count];
            for (int i = 0; i < rays.Count; i++)
            {
                colors[i] = rays[i].Data.Color;
                colors[i].a = 1.0f; // The ray tracer messes up the alpha channel, but it should always be 1.
            }
            return colors;
        }
        /// <summary>
        /// Get the material used to render rays of the given <see cref="RTRay.RayType"/>.
        /// </summary>
        /// <param name="contribution"> The fraction of the pixel-color this ray adds to it. </param>
        /// <param name="type"> What type of ray to get the material for. </param>
        /// <param name="color"> The color this ray contributes to the pixel-color </param>
        /// <returns>
        /// The <see cref="Material"/> for <paramref name="contribution"/>, <paramref name="type"/> and <paramref name="color"/>.
        /// </returns>
        public Material GetRayMaterial(float contribution, RTRay.RayType type, Color color)
        {
            if (RayTransparencyEnabled && contribution <= RayTransThreshold)
            {
                if (RayColorContributionEnabled)
                    return GetRayColorMaterialTransparent(color.r, color.g, color.b, Mathf.Pow(contribution, RayTransExponent));
                else
                    return GetRayTypeMaterialTransparent(type, Mathf.Pow(contribution, RayTransExponent));
            }
            else
            {
                if (RayColorContributionEnabled)
                    return GetRayColorMaterial(color.r, color.g, color.b);
                else
                    return GetRayTypeMaterial(type);
            }
        }

        /// <summary>
        /// Get the material used to render rays of the given <see cref="RTRay.RayType"/>.
        /// </summary>
        /// <param name="type"> What type of ray to get the material for. </param>
        /// <returns>
        /// The <see cref="Material"/> for <paramref name="type"/>. The material for <see cref="RTRay.RayType.NoHit"/>
        /// is returned if <paramref name="type"/> is not a recognized <see cref="RTRay.RayType"/>.
        /// </returns>
        private Material GetRayTypeMaterial(RTRay.RayType type)
        {
            switch (type)
            {
                case RTRay.RayType.NoHit:
                    return noHitMaterial;
                case RTRay.RayType.Reflect:
                    return reflectMaterial;
                case RTRay.RayType.Refract:
                    return refractMaterial;
                case RTRay.RayType.Normal:
                    return normalMaterial;
                case RTRay.RayType.Shadow:
                    return shadowMaterial;
                case RTRay.RayType.Light:
                    return lightMaterial;
                default:
                    Debug.LogError("Unrecognized ray type " + type + "!");
                    return errorMaterial;
            }
        }

        /// <summary>
        /// Get the transparent material used to render rays of the given <see cref="RTRay.RayType"/>.
        /// </summary>
        /// <param name="type"> What type of ray to get the material for. </param>
        /// <param name="transparency"> How transparent the ray should be. </param>
        /// <returns>
        /// The transparent <see cref="Material"/> for <paramref name="type"/>.
        /// </returns>
        private Material GetRayTypeMaterialTransparent(RTRay.RayType type, float transparency)
        {
            if (transparency > 1.0f)
                Debug.LogError("Transparency bigger than 1: " + transparency.ToString());

            switch (type)
            {
                case RTRay.RayType.NoHit:
                    return noHitMaterial;
                case RTRay.RayType.Reflect:
                    return reflectMaterialTransparentArray[Mathf.RoundToInt(transparency * (transparencyRange - 1))];
                case RTRay.RayType.Refract:
                    return refractMaterialTransparentArray[Mathf.RoundToInt(transparency * (transparencyRange - 1))];
                case RTRay.RayType.Normal:
                    return normalMaterialTransparentArray[Mathf.RoundToInt(transparency * (transparencyRange - 1))];
                case RTRay.RayType.Shadow:
                    return shadowMaterialTransparentArray[Mathf.RoundToInt(transparency * (transparencyRange - 1))];
                case RTRay.RayType.Light:
                    return lightMaterialTransparentArray[Mathf.RoundToInt(transparency * (transparencyRange - 1))];
                default:
                    Debug.LogError("Unrecognized ray type " + type + "!");
                    return errorMaterial;
            }
        }

        /// <summary>
        /// Get the material used to render rays of the given Ray based on its color.
        /// </summary>
        /// <param name="r"> The red component of the color </param>
        /// <param name="g"> The green component of the color </param>
        /// <param name="b"> The blue component of the color </param>
        /// <returns>
        /// The <see cref="Material"/> for <paramref name="r"/>, <paramref name="g"/> and <paramref name="b"/>.
        /// </returns>
        private Material GetRayColorMaterial(float r, float g, float b)
        {
            int idxr = Mathf.RoundToInt(r * (colorN - 1));
            int idxg = Mathf.RoundToInt(g * (colorN - 1));
            int idxb = Mathf.RoundToInt(b * (colorN - 1));

            // To optimize loading-performance, these materials are constucted on-demand.
            // To optimize runtime-performance, once they're made, they're saved
            if (!colorRayMaterialArray[idxr, idxg, idxb])
                colorRayMaterialArray[idxr, idxg, idxb] = new Material(colorRayMaterial)
                { color = new Color(idxr * colorStep, idxg * colorStep, idxb * colorStep, 1f) };

            return colorRayMaterialArray[idxr, idxg, idxb];
        }

        /// <summary>
        /// Get the transparent material used to render rays of the given Ray based on its color.
        /// </summary>
        /// <param name="r"> The red component of the color </param>
        /// <param name="g"> The green component of the color </param>
        /// <param name="b"> The blue component of the color </param>
        /// <param name="transparency"> How transparent the ray should be. </param>
        /// <returns> 
        /// The transparent <see cref="Material"/> for <paramref name="r"/>, <paramref name="g"/> and <paramref name="b"/>.
        /// </returns>
        private Material GetRayColorMaterialTransparent(float r, float g, float b, float transparency)
        {
            int idxr = Mathf.RoundToInt(r * (colorN - 1));
            int idxg = Mathf.RoundToInt(b * (colorN - 1));
            int idxb = Mathf.RoundToInt(g * (colorN - 1));
            int idxa = Mathf.RoundToInt(transparency * (transparencyRange - 1));

            // To optimize load-performance, these materials are constucted on-demand.
            // To further optimize performance, once they're made, they're saved.
            if (!colorRayMaterialTransparentArray[idxr, idxg, idxb, idxa])
            {
                colorRayMaterialTransparentArray[idxr, idxg, idxb, idxa] = new Material(colorRayMaterialTransparent)
                { color = new Color(idxr * colorStep, idxg * colorStep, idxb * colorStep, colorRayMaterialTransparent.color.a) };
                colorRayMaterialTransparentArray[idxr, idxg, idxb, idxa].SetFloat("_Ambient", (idxa + 1) * (1f / transparencyRange));
            }

            return colorRayMaterialTransparentArray[idxr, idxg, idxb, idxa];
        }

        public void SelectRay(Vector2Int rayCoordinates)
        {
            selectedRayCoordinates = rayCoordinates;
            hasSelectedRay = true;
            Reset = true;
        }

        public void DeselectRay()
        {
            hasSelectedRay = false;
            Reset = true;
        }

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            rays = new List<TreeNode<RTRay>>();
            // Generate range of transparent materials
            MakeTransparentMaterials(ref reflectMaterialTransparent, ref reflectMaterialTransparentArray);
            MakeTransparentMaterials(ref refractMaterialTransparent, ref refractMaterialTransparentArray);
            MakeTransparentMaterials(ref normalMaterialTransparent, ref normalMaterialTransparentArray);
            MakeTransparentMaterials(ref shadowMaterialTransparent, ref shadowMaterialTransparentArray);
            MakeTransparentMaterials(ref lightMaterialTransparent, ref lightMaterialTransparentArray);

            rayObjectPool = new RayObjectPool(rayPrefab, initialRayPoolSize, transform);
            Reset = true;

            rtSceneManager = RTSceneManager.Get();
            rayTracer = UnityRayTracer.Get();

            rtSceneManager.Scene.OnSceneChanged += () => { shouldUpdateRays = true; };
            rayTracer.OnRayTracerChanged += () => { shouldUpdateRays = true; };
        }

        private void MakeTransparentMaterials(ref Material baseMaterial, ref Material[] arrayMaterial)
        {
            for (int i = 0; i < transparencyRange; i++)
            {
                arrayMaterial[i] = new Material(baseMaterial);
                arrayMaterial[i].SetFloat("_Ambient", (i + 1) * (1f / transparencyRange));
            }
        }

        private void FixedUpdate()
        {
            rayObjectPool.SetAllUnused();
            
            if(shouldUpdateRays)
                UpdateRays();

            // Determine the selected ray.
            if (hasSelectedRay)
            {
                int width = rtSceneManager.Scene.Camera.ScreenWidth;
                int index = selectedRayCoordinates.x + width * selectedRayCoordinates.y;
                selectedRay = rays[index];
            }

            if (Animate)
                DrawRaysAnimated();
            else
                DrawRays();
            
            rayObjectPool.DeactivateUnused();
        }

        /// <summary>
        /// Get new ray trees from the ray tracer.
        /// </summary>
        public void UpdateRays()
        {
            rays = rayTracer.Render();
            rtSceneManager.UpdateImage(GetRayColors());
            shouldUpdateRays = false;
        }

        /// <summary>
        /// Returns the radius of the ray.
        /// </summary>
        /// <param name="rayTree">ray to get radius from</param>
        /// <returns></returns>
        private float GetRayRadius(TreeNode<RTRay> rayTree)
        {
            if (rayDynamicRadiusEnabled)
                return rayMinRadius + rayTree.Data.Contribution * (rayMaxRadius - rayMinRadius);
            else
                return rayRadius;
        }

        /// <summary>
        /// Draw <see cref="rays"/> in full.
        /// </summary>
        private void DrawRays()
        {
            if (!ShowRays)
                return;

            // If we have selected a ray we only draw its ray tree.
            if (hasSelectedRay)
            {
                foreach (var ray in selectedRay.Children) // Skip the zero-length base-ray 
                    DrawRayTree(ray);
            }
            // Otherwise we draw all ray trees.
            else
            {
                foreach (var pixel in rays)
                    foreach (var ray in pixel.Children) // Skip the zero-length base-ray 
                        DrawRayTree(ray);
            }

        }

        private void DrawRayTree(TreeNode<RTRay> rayTree)
        {
            if ((HideNoHitRays && rayTree.Data.Type == RTRay.RayType.NoHit) ||
                (HideNegligibleRays && rayTree.Data.Contribution <= rayHideThreshold))
                return;

            RayObject rayObject = rayObjectPool.GetRayObject();
            rayObject.Ray = rayTree.Data;
            rayObject.Draw(GetRayRadius(rayTree));

            if (!rayTree.IsLeaf())
            {
                foreach (var child in rayTree.Children)
                    DrawRayTree(child);
            }
        }

        /// <summary>
        /// Draw a part of <see cref="rays"/> up to <see cref="distanceToDraw"/>. The part drawn grows each frame.
        /// </summary>
        private void DrawRaysAnimated()
        {
            if (!ShowRays)
                return;

            // Reset the animation if we are looping or if a reset was requested.
            if (animationDone && Loop || Reset)
            {
                distanceToDraw = 0.0f;
                rayTreeToDraw = 0;
                animationDone = false;
                Reset = false;
            }

            // Animate all ray trees if we are not done animating already.
            if (!animationDone)
            {
                distanceToDraw += Speed * Time.deltaTime;
                animationDone = true; // Will be reset to false if one tree is not finished animating.

                // If we have selected a ray we only draw its ray tree.
                if (hasSelectedRay)
                {
                    foreach (var ray in selectedRay.Children) // Skip the zero-length base-ray 
                        animationDone &= DrawRayTreeAnimated(ray, distanceToDraw);
                }
                // If specified we animate the ray trees sequentially (pixel by pixel).
                else if (animateSequentially)
                {
                    // Draw all previous ray trees in full.
                    for (int i = 0; i < rayTreeToDraw; ++i)
                        foreach (var ray in rays[i].Children) // Skip the zero-length base-ray 
                            DrawRayTree(ray);

                    // Animate the current ray tree. If it is now fully drawn we move on to the next one.
                    bool treeDone = true;
                    foreach(var ray in rays[rayTreeToDraw].Children) // Skip the zero-length base-ray 
                        treeDone &= DrawRayTreeAnimated(ray, distanceToDraw);

                    if (treeDone)
                    {
                        distanceToDraw = 0.0f;
                        ++rayTreeToDraw;
                    }

                    animationDone = treeDone && rayTreeToDraw >= rays.Count;
                }
                // Otherwise we animate all ray trees.
                else
                {
                    foreach (var pixel in rays)
                        foreach (var rayTree in pixel.Children) // Skip the zero-length base-ray 
                            animationDone &= DrawRayTreeAnimated(rayTree, distanceToDraw);
                }
            }
            // Otherwise we can just draw all rays in full.
            else
                DrawRays();
        }

        private bool DrawRayTreeAnimated(TreeNode<RTRay> rayTree, float distance)
        {
            if ((HideNoHitRays && rayTree.Data.Type == RTRay.RayType.NoHit) ||
                (HideNegligibleRays && rayTree.Data.Contribution < rayHideThreshold))
                return true;

            RayObject rayObject = rayObjectPool.GetRayObject();
            rayObject.Ray = rayTree.Data;
            rayObject.Draw(GetRayRadius(rayTree), distance);

            float leftover = distance - rayObject.DrawLength;

            // If this ray is not at its full length we are not done animating.
            if (leftover <= 0.0f)
                return false;
            // If this ray is at its full length and has no children we are done animating.
            if (rayTree.IsLeaf())
                return true;

            // Otherwise we start animating the children.
            bool done = true;
            foreach (var child in rayTree.Children)
                done &= DrawRayTreeAnimated(child, leftover);
            return done;
        }
    }
}
