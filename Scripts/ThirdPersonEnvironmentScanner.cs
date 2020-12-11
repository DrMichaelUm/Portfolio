using System;
using Bimicore.BSG.CoolTools;
using Bimicore.Lib;
using UnityEngine;
using UnityEngine.Assertions;

namespace Bimicore.BSG.ThirdPerson
{
	public class ThirdPersonEnvironmentScanner : MonoBehaviour
	{
// Disabled warnings like 'Field is never assigned to, and will always have its default value'
// because structs initialization is in Inspector
#pragma warning disable CS0649

		[SerializeField] private Vector3 playerUp;
		[SerializeField] private Vector3 playerBottom;
		
		[Space]
		
		[SerializeField] private GroundCheck groundCheck;

		[Space] 
		
		[SerializeField] private ObstacleCheck obstacleCheck;

		[Space] 
		
		[SerializeField] private RaySettings upSpaceCheck;
			
		private ThirdPersonMovementPhysicalProperties _physicalProperties;
		private RaycastHit? _obstacleHit;
		private RaycastHit[] _collisionHits;

#pragma warning restore

		private void Awake()
		{
			_physicalProperties = GetComponent<ThirdPersonMovementPhysicalProperties>();
		}


		//TODO: resource-intensive debugging
		private void Update()
		{
			DebugPlus.LogOnScreen("OnGround: " + IsGrounded());
			DebugPlus.LogOnScreen("GroundType: " + GetGroundTypeBelow());
			DebugPlus.LogOnScreen("IsOnSlide: " + IsOnSlide());
		}

		// Warning: method can return true if player are very close to the ground. Modify distToGround to control that.
		public bool IsGrounded()
		{
			var supposedGround = GetGroundHit(groundCheck.downRay);

			//TODO: how much getting collider from hit is less optimized than check only for hit?
			if (supposedGround.collider)
			{
				if (LayersCoolTool.IsSameLayer(groundCheck.groundMask, supposedGround.collider.gameObject.layer))
				{
					return true;
				}
			}

			return false;
		}

		private RaycastHit GetGroundHit (RaySettings raySettings)
		{
			Physics.Raycast(raySettings.origin.position,
			                -transform.up,
			                out var hit,
			                raySettings.checkDistance,
			                groundCheck.groundMask);

			return hit;
		}

		public GroundType GetGroundTypeBelow()
		{
			GroundType groundType = GetGroundTypeByRay(GetGroundCheckRay());

			return groundType;
		}

		private RaySettings GetGroundCheckRay()
		{
			GroundType groundTypeInFront = GetGroundTypeByRay(groundCheck.slope.frontRay);

			// If the ground in front is lower (or absent) than the current ground, then hold on to the last on the current ground.
			if (IsGroundGoesDownOrAbsent(groundTypeInFront))
			{
				return groundCheck.slope.backRay;
			}

			return groundCheck.slope.frontRay;
		}

		private GroundType GetGroundTypeByRay (RaySettings raySettings)
		{
			var ground = GetGroundHit(raySettings);

			float groundAngle = GetGroundAngle(ground);

			return GetGroundTypeByAngle(groundAngle);
		}

		public float GetGroundAngle()
		{
			RaycastHit hit = GetGroundHit(GetGroundCheckRay());

			// Get the SIGNED initVelocityAngle to slope and multiply it with -1 to define difference btw ascend and descend
			float slopeAngle = Vector2.SignedAngle(hit.normal, Vector2.up) * -1;

			return slopeAngle;
		}

		private float GetGroundAngle (RaycastHit hit) =>
			Vector2.Angle(hit.normal, GetMovementDirectionBasedOnVelocity());


		public Vector3 GetMovementDirectionBasedOnVelocity()
		{
			Vector3 directionRelativeToMovement;

			if (_physicalProperties.targetVelocityDirection.x != 0)
				directionRelativeToMovement = new Vector3(Mathf.Sign(_physicalProperties.targetVelocity.x), 0, 0);
			else // if obj doesn't move, use face direction
				directionRelativeToMovement = new Vector3(transform.forward.x, 0, 0);

			return directionRelativeToMovement;
		}

		private GroundType GetGroundTypeByAngle (float groundAngle)
		{
			if (Math.Abs(groundAngle - 90) <= groundCheck.slope.groundAngleTolerance)
				return GroundType.Flat;

			if (groundAngle < 90 - groundCheck.slope.groundAngleTolerance && groundAngle > 0)
				return GroundType.Descent;

			if (groundAngle > 90 + groundCheck.slope.groundAngleTolerance)
				return GroundType.Ascent;

			return GroundType.NoGround;
		}

		private bool IsGroundGoesDownOrAbsent (GroundType groundTypeInFront) =>
			groundTypeInFront == GroundType.NoGround || groundTypeInFront == GroundType.Descent;

		public bool IsOnSlide()
		{
			if (GetGroundTypeBelow() == GroundType.Ascent || GetGroundTypeBelow() == GroundType.Descent)
			{
				return Mathf.Abs(GetGroundAngle()) >= groundCheck.slide.minAngleToSlide &&
				       Mathf.Abs(GetGroundAngle()) <= groundCheck.slide.maxAngleToSlide;
			}

			return false;
		}

		public int GetSlideDirection() => (int) Mathf.Sign(GetGroundHit(GetGroundCheckRay()).normal.x);

		public bool IsWallInDirection (Vector3 direction) => IsObstacleHitRecorded(direction, obstacleCheck.ray.checkDistance);

		public bool IsObstacleScaleMatch (float length, float height, Vector3 direction, float distance = 0f)
		{
			if (distance == 0f)
				distance = obstacleCheck.ray.checkDistance + 1f;
			
			if (IsObstacleHitRecorded(direction, distance))
			{
				return IsObstacleScaleMatch(length, height);
			}

			return false;
		}

		private bool IsObstacleScaleMatch ( float length, float height) => _obstacleHit.Value.collider.transform.localScale.x <= length && _obstacleHit.Value.collider.transform.localScale.y <= height;

		public bool IsObstacleHitRecorded (Vector3 direction, float distance)
		{
			if (Physics.CapsuleCast(_physicalProperties.CapsuleRayParams.topPoint,
			                        _physicalProperties.CapsuleRayParams.bottomPoint, _physicalProperties.Cl.radius,
			                        direction, out var wallHit,
			                        distance, groundCheck.groundMask))
			{
				float wallAngle = Vector2.Angle(wallHit.normal, Vector2.up);

				if (wallAngle >= groundCheck.wall.minWallAngle && wallAngle <= groundCheck.wall.maxWallAngle)
				{
					_obstacleHit = wallHit;
					return true;
				}
			}

			_obstacleHit = null;
			return false;
		}

		public bool IsFreeAbove() => IsFreeAbove(upSpaceCheck.checkDistance);

		public bool IsFreeAbove (float checkDistance)
		{
			if (Physics.Raycast(upSpaceCheck.origin.position,
			                    Vector3.up,
			                    out var hit,
			                    checkDistance))
			{
				return false;
			}

			return true;
		}

		//TODO: resource-intensive debugging
		private void OnDrawGizmos()
		{
			// Ground check ray
			Gizmos.color = Color.red;

			Gizmos.DrawRay(groundCheck.downRay.origin.position + Vector3.up * 0.2f,
			               -Vector3.up * (groundCheck.downRay.checkDistance + 0.1f));

			// Front slope check ray
			Gizmos.color = Color.black;

			Gizmos.DrawRay(groundCheck.slope.frontRay.origin.position,
			               -transform.up * groundCheck.slope.frontRay.checkDistance);

			// Back slope check ray
			Gizmos.color = Color.black;

			Gizmos.DrawRay(groundCheck.slope.backRay.origin.position,
			               -transform.up * groundCheck.slope.backRay.checkDistance);

			Gizmos.color = Color.red;

			if (obstacleCheck.debugDraw && _physicalProperties != null)
			{
				GizmosDrawer.DrawWireCapsule(_physicalProperties.CapsuleRayParams.topPoint + transform.forward * obstacleCheck.ray.checkDistance,
				                             _physicalProperties.CapsuleRayParams.bottomPoint + transform.forward * obstacleCheck.ray.checkDistance,
				                             _physicalProperties.Cl.radius,
				                             _physicalProperties.Cl.height, Color.red);
			}

			Gizmos.color = Color.white;
		}

#pragma warning disable CS0649

		[Serializable]
		private struct GroundCheck
		{
			public LayerMask groundMask;
			public RaySettings downRay;
			[Space] public SlopeCheck slope;
			[Space] public SlideCheck slide;
			[Space] public WallCheck wall;
		}

		[Serializable]
		private struct SlopeCheck
		{
			public float groundAngleTolerance;
			public RaySettings frontRay;
			public RaySettings backRay;
		}

		[Serializable]
		private struct SlideCheck
		{
			public float minAngleToSlide;
			public float maxAngleToSlide;
		}

		[Serializable]
		private struct WallCheck
		{
			public RaySettings forwardRay;
			public float minWallAngle;
			public float maxWallAngle;
		}

		[Serializable]
		private struct ObstacleCheck
		{
			public LayerMask obstacleMask;
			public RaySettings ray;
			public bool debugDraw;
		}

		[Serializable]
		private struct RaySettings
		{
			public Transform origin;
			public float checkDistance;
		}

#pragma warning restore
	}
}
