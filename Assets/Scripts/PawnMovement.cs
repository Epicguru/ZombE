using UnityEngine;

namespace ZombE
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PawnMovement : MonoBehaviour
    {
        public Rigidbody2D Body
        {
            get
            {
                if (_body == null)
                    _body = GetComponent<Rigidbody2D>();
                return _body;
            }
        }
        private Rigidbody2D _body;

        public Vector2 Direction;
        public float Speed = 7f;

        public float InputMagnitude = 100f;

        private Vector2 startPos;

        private void Update()
        {
            if (!Application.isMobilePlatform)
            {
                Direction = new Vector2();
                Direction.x = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
                Direction.y = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
            }
            else
            {
                Direction = new Vector2();
                if (Input.GetMouseButtonDown(0))
                {
                    startPos = Input.mousePosition;
                }
                if (Input.GetMouseButton(0))
                {
                    Vector2 delta = (Vector2)Input.mousePosition - startPos;

                    Direction = delta.normalized * (Mathf.Clamp01(delta.magnitude / InputMagnitude));
                }
            }
        }

        private void FixedUpdate()
        {
            Vector2 dir = Direction;
            if(Direction.sqrMagnitude > 1f)
            {
                dir = Direction.normalized;
            }

            Body.velocity = dir * Speed;
        }
    }
}
