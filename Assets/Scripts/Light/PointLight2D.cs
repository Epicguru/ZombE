using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

namespace ZombE
{
    public class PointLight2D : MonoBehaviour
    {
        private static List<Vector2> tempPoints = new List<Vector2>();
        private static List<Vector2> visiblePoints = new List<Vector2>();
        private static SortedList<float, Vector3> finalPoints = new SortedList<float, Vector3>();
        private static Dictionary<Vector2, Collider2D> pointOwnership = new Dictionary<Vector2, Collider2D>();
        private static Dictionary<Collider2D, float> colliderAngles = new Dictionary<Collider2D, float>();
        private static CircleCollider2D CircleCollider
        {
            get
            {
                if(_cc == null)
                {
                    var go = new GameObject("Temp Circle Collider");
                    _cc = go.AddComponent<CircleCollider2D>();
                    _cc.radius = _cr;
                }
                return _cc;
            }
        }
        private static CircleCollider2D _cc;
        private static float _cr;

        public Collider2D[] Colliders;
        public Light2D Light;

        [Header("Properties")]
        public float Radius = 10f;
        public Color Color = Color.white;

        [Header("Performace")]
        [Range(0, 120)]
        public int AproximationCastCount = 0;

        [Header("Graphics")]
        public Texture2D LightCookie;

        [Header("Stability")]
        public float CircleRadius = 0.04f; // 4cm
        public float EdgeDetectRayOffset = 0.002f; // 1mm
        public EdgeDetectionMode EdgeDetection = PointLight2D.EdgeDetectionMode.Raycast;

        [Header("Debugging")]
        public DebugDrawMode DebugMode = 0;
        public bool QueriesStartInColliders = true;

        private GameObject tempColliderObject;

        public enum DebugDrawMode
        {
            None = 0,
            AllPoints = 1,
            VisiblePoints = 2,
            FinalPoints = 4
        }

        public enum EdgeDetectionMode
        {
            Raycast,
            InteriorPoint
        }

        private void Awake()
        {
            _cr = CircleRadius;

            // Needs to be turned off. Should already be disabled by default in new unity versions.
            Physics2D.autoSyncTransforms = false;
        }

        private void LateUpdate()
        {
            Debug.Log(Colliders[0].OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition)));

            // Basic state checks.
            if (Colliders == null)
                return;

            // Clear point lists.
            visiblePoints.Clear();
            tempPoints.Clear();
            finalPoints.Clear();
            pointOwnership.Clear();
            colliderAngles.Clear();

            // Update colliders positions.
            Physics2D.SyncTransforms();
            Physics2D.queriesStartInColliders = false;

            // TODO add more edge points and make edge points 'slide' towards the source when blocked.
            // Add the static 4 edge points.
            float rp = Radius + 1; // A bit further than the real range.
            tempPoints.Add(transform.position + new Vector3(-rp, rp));
            tempPoints.Add(transform.position + new Vector3(rp, rp));
            tempPoints.Add(transform.position + new Vector3(rp, -rp));
            tempPoints.Add(transform.position + new Vector3(-rp, -rp));

            // Occlude and then add the corners.
            int cornersAdded = 0;
            foreach (var point in tempPoints)
            {
                CircleCollider.offset = point;
                var hit = Physics2D.Linecast(transform.position, point);
                if (hit.collider == CircleCollider)
                {
                    visiblePoints.Add(point);
                    cornersAdded++;
                }
                else
                {
                    visiblePoints.Add(hit.point);
                    cornersAdded++;
                }
            }
            tempPoints.Clear();

            bool drawAllPoints = DebugMode.HasFlag(DebugDrawMode.AllPoints);

            // Get all edge vertices from all colliders...
            foreach (var collider in Colliders)
            {
                if (collider == null)
                    continue;

                // Work out the angle to the centre of the collider, and store it.
                Vector2 colliderCentre = collider.GetCentre();
                Vector2 centreDiff = colliderCentre - (Vector2)transform.position;
                colliderAngles.Add(collider, Mathf.Atan2(centreDiff.y, centreDiff.x));

                // Get the points for this collider.
                collider.GetCastPoints(transform.position, tempPoints);

                // Raycast to all vertices.
                for (int i = 0; i < tempPoints.Count; i++)
                {
                    Vector2 point = tempPoints[i];

                    // Debug draw.
                    if (drawAllPoints)
                    {
                        Debug.DrawLine(transform.position, point, Color.green);
                    }

                    // Remove all points out of range.
                    float sqrDst = (point - (Vector2)transform.position).sqrMagnitude;
                    if (sqrDst > Radius * Radius)
                    {
                        continue;
                    }

                    CircleCollider.offset = point;
                    var hit = Physics2D.Linecast(transform.position, point);
                    if (hit && hit.collider == CircleCollider)
                    {
                        visiblePoints.Add(point);

                        // Record point ownership.
                        pointOwnership.Add(point, collider);
                    }
                }

                // Clear the points list after every collider.
                tempPoints.Clear();
            }

            CircleCollider.offset = new Vector2(10000f, 10000f); // Out of sight, out of mind.        

            Physics2D.queriesStartInColliders = true;

            // Check which visible points are edges...
            for (int i = 0; i < visiblePoints.Count; i++)
            {
                Vector2 point = visiblePoints[i];

                // If this point is a corner, skip edge detection. It isn't an edge because it is a floating point!
                if (i < cornersAdded)
                {
                    AddFinalPoint(point);
                    continue;
                }

                Vector2 dir = (point - (Vector2)transform.position);
                Vector2 dirNormalized = dir.normalized;
                Vector2 start = point + dirNormalized * EdgeDetectRayOffset;

                const float NUDGE = 0.0001f;

                var hit = Physics2D.Raycast(start, dir, Radius); // Note that the range value here is just to avoid calculating the remaining distance.
                LogRaycast(start, hit);

                bool inCollider = false;
                if (EdgeDetection == EdgeDetectionMode.InteriorPoint)
                    inCollider = pointOwnership[point].OverlapPoint(start);

                bool airBehind = EdgeDetection == EdgeDetectionMode.Raycast ? !hit : !inCollider;

                if (airBehind)
                {
                    // It's an edge.
                    // There is nothing behind it.
                    // Add a new point that is behind it.

                    bool clockwise = IsClockwise(point);
                    AddFinalPoint((Vector2)transform.position + dirNormalized * Radius * 1.2f, clockwise ? -NUDGE : NUDGE);
                }
                else
                {
                    if (EdgeDetection == EdgeDetectionMode.Raycast ? hit.point == start : inCollider)
                    {
                        // Can't go in there!
                        // No extra point added here.
                    }
                    else
                    {
                        // It's an edge, with something behind it.
                        // Add a new point on whatever it hit behind.
                        bool clockwise = IsClockwise(point);
                        AddFinalPoint(hit.point, clockwise ? -NUDGE : NUDGE);
                    }
                }

                AddFinalPoint(point);
            }

            Physics2D.queriesStartInColliders = false;
            // Do aproximation casts.
            // This is simply firing rays outwards in all directions, and adding any point that they hit.
            // Makes mesh generation and rendering much slower, but can help avoid occluded vertices creating incorrect lighting.
            // By default, no rays are casted, but up to 120 per frame are allowed.
            for (int i = 0; i < AproximationCastCount; i++)
            {
                float p = (float)i / AproximationCastCount;
                float angle = p * Mathf.PI * 2f;

                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                var hit = Physics2D.Raycast(transform.position, direction, Radius);

                // If we hit something, add that point.
                if (hit)
                {
                    AddFinalPoint(hit.point);
                }

            }

            DrawDebug(tempPoints);

            var array = finalPoints.Values.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = array[i] - transform.position;
            }
            SetPoints(array);
        }

        private void SetPoints(Vector3[] array)
        {
            if (array == null || array.Length < 3)
            {
                Debug.LogWarning($"Tried to set invalid array. Length: {array?.Length.ToString() ?? "(null array)"}");
                return;
            }
            Light.GetType().GetField("m_ShapePath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Light, array);
        }

        private bool IsClockwise(Vector2 point)
        {
            Collider2D collider = pointOwnership[point];
            Debug.Assert(collider != null);

            Vector2 diffToPoint = point - (Vector2)transform.position;

            float centreAngle = colliderAngles[collider] * Mathf.Rad2Deg;
            float pointAngle = Mathf.Atan2(diffToPoint.y, diffToPoint.x) * Mathf.Rad2Deg;

            float net = Mathf.DeltaAngle(centreAngle, pointAngle);

            bool isClockwise = net < 0f;

            return isClockwise;
        }

        private void AddFinalPoint(Vector2 point, float angleOffset = 0f)
        {
            float angle = CalcAngle(point) + angleOffset;
            while (finalPoints.ContainsKey(angle))
            {
                angle += 0.00001f; // Since the two points are in the same place, it doesn't really matter which forms the triangle.
            }

            finalPoints.Add(angle, point);
        }

        private float CalcAngle(Vector2 point)
        {
            Vector2 diff = point - (Vector2)transform.position;
            return Mathf.Atan2(diff.y, diff.x);
        }

        private void DrawDebug(List<Vector2> allPoints)
        {
            if (DebugMode.HasFlag(DebugDrawMode.VisiblePoints))
            {
                foreach (var point in visiblePoints)
                {
                    Debug.DrawLine(transform.position, point, Color.magenta);
                }
            }
            if (DebugMode.HasFlag(DebugDrawMode.FinalPoints))
            {
                int i = 0;
                foreach (var pair in finalPoints)
                {
                    bool first = i == 0;
                    bool last = i == finalPoints.Count - 1;

                    Vector2 point = pair.Value;
                    const float SIZE = 0.03f;
                    Color c = first ? Color.green : last ? Color.red : Color.yellow;
                    Debug.DrawLine(point + new Vector2(-SIZE, SIZE), point + new Vector2(SIZE, -SIZE), c);
                    Debug.DrawLine(point + new Vector2(-SIZE, -SIZE), point + new Vector2(SIZE, SIZE), c);

                    i++;
                }
            }
        }

        private void LogRaycast(Vector2 origin, RaycastHit2D hit)
        {
            if (hit)
            {
                Debug.DrawLine(origin, hit.point, Color.red);
                Debug.DrawLine(hit.point, hit.point + hit.normal * 0.1f, Color.cyan);
            }
            else
            {
                Debug.DrawLine(origin, origin + Vector2.up * 0.2f, Color.magenta);
            }
        }

        private void OnDrawGizmosSelected()
        {
            float oldA = Color.a;
            Color.a = 0.3f;
            Gizmos.color = Color;
            Color.a = oldA;

            Gizmos.DrawWireSphere(transform.position, Radius);

        }
    }
}

