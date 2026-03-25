using System.Collections;
using UnityEngine;

namespace Camera
{
    [DisallowMultipleComponent]
    public class CameraFollowPosition : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("If null, the script will automatically find the first GameObject tagged 'Player'.")]
        [SerializeField]
        private Transform target;

        [Header("Follow")] [Tooltip("Higher = faster follow. Typical range: 3–10")] [SerializeField]
        private float followSpeed = 6f;

        [Tooltip("Whether to use FixedUpdate (recommended if player moves in FixedUpdate).")] [SerializeField]
        private bool useFixedUpdate = false;

        private Vector3 offset;
        private Vector3 actualOffset;

        private Quaternion rotation;
        private Quaternion actualRotation;
        
        private bool initialized;

        private void Awake()
        {
            ResolveTarget();
        }

        private void Start()
        {
            InitializeStates();
            
            // For testing: change offset after 5 seconds
            StartCoroutine(TransitionToSideTest());
        }

        public void SetNewOffset(Vector3 newOffset)
        {
            offset = newOffset;
            rotation = Quaternion.FromToRotation(Vector3.forward, -newOffset.normalized);
        }

        public void SetFollowTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
                offset = transform.position - target.position;
            initialized = target != null;
        }

        private void ResolveTarget()
        {
            if (target != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                Debug.LogWarning($"{nameof(CameraFollowPosition)}: No GameObject tagged 'Player' found.");
        }

        private void InitializeStates()
        {
            if (target == null) return;

            offset = transform.position - target.position;
            actualOffset = offset;
            rotation = transform.rotation;
            actualRotation = rotation;
            initialized = true;
        }

        private IEnumerator TransitionToSideTest()
        {

            yield return new WaitForSecondsRealtime(5f);
            SetNewOffset(new Vector3(0f, 3f, -10f));
        }

        private void Update()
        {
            if (!useFixedUpdate)
                Follow(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (useFixedUpdate)
                Follow(Time.fixedDeltaTime);
        }

        private void Follow(float deltaTime)
        {
            if (!initialized || target == null) return;

            {
                actualOffset = Vector3.Lerp(
                    actualOffset,
                    offset,
                    1f - Mathf.Exp(-followSpeed * deltaTime)
                );
                
                actualRotation = Quaternion.Slerp(
                    actualRotation,
                    rotation,
                    1f - Mathf.Exp(-followSpeed * deltaTime)
                );
            }
            
            Vector3 desiredPosition = target.position + actualOffset;

            // Position-only smoothing (orientation untouched)
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                1f - Mathf.Exp(-followSpeed * deltaTime)
            );
        }
    }
}
