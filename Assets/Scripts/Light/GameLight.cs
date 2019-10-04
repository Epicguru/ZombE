
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

namespace ZombE
{
    [RequireComponent(typeof(Light2D))]
    public class GameLight : MonoBehaviour
    {
        public Light2D Light
        {
            get
            {
                if (_light == null)
                    _light = GetComponent<Light2D>();
                return _light;
            }
        }
        private Light2D _light;

        private void Awake()
        {
            //foreach (var prop in Light.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            //    Debug.Log(prop);

            Light.lightType = Light2D.LightType.Freeform;
        }

        private void SetPoints(Vector3[] array)
        {
            Light.GetType().GetField("m_ShapePath", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Light, new Vector3[] { Vector3.zero, Vector3.one, new Vector3(-1, 5, 0) });
        }
    }
}
