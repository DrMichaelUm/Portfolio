using System.Threading.Tasks;
using UnityEngine;

namespace Bimicore.BSG.ThirdPerson
{
	public abstract class MovementAbstractAbility : ThirdPersonAbility
	{
		protected const float COMPARE_VELOCITY_TOLERANCE = 0.5f;
		
		protected bool IsIncreasingVelocity { get; set; }
		protected bool IsDecreasingVelocity { get; set; }

		protected ThirdPersonMovementPhysicalProperties physicalProperties;
		protected ThirdPersonRotation thirdPersonRotation;
		protected ThirdPersonEnvironmentScanner environmentScanner;

		protected override void Awake()
		{
			base.Awake();
			physicalProperties = GetComponent<ThirdPersonMovementPhysicalProperties>();
			thirdPersonRotation = GetComponent<ThirdPersonRotation>();
			environmentScanner = GetComponent<ThirdPersonEnvironmentScanner>();
		}

		protected virtual void CommonEnter()
		{
		}

		protected virtual void CommonTick()
		{
		}

		protected virtual void CommonFixedTick()
		{
			Move (physicalProperties.targetVelocity);
		}

		protected virtual void CommonExit()
		{
			IsIncreasingVelocity = false;
			IsDecreasingVelocity = false;
		}

		private void Move (Vector3 velocity)
		{
			physicalProperties.Rb.velocity = velocity;
		}

		protected virtual void CalculateTargetVelocityDirection (float movementDirection)
		{
			Vector3 newTargetVelocityDirection = Vector3.right * movementDirection;
			//Multiply on -1 to turn velocity in opposite angle from slope because we need to climb it
			float groundAngle = environmentScanner.GetGroundSignedAngle() * -1;

			physicalProperties.targetVelocityDirection =
				Quaternion.Euler (0, 0, groundAngle) * newTargetVelocityDirection;
		}

		protected virtual void SetTargetVelocity (float speed)
		{
			physicalProperties.targetVelocity = physicalProperties.targetVelocityDirection * speed;
		}

		protected void AlignTargetVelocity()
		{
			physicalProperties.targetVelocity.AlignRotationWith (physicalProperties.targetVelocityDirection);
		}

		protected async Task IncreaseTargetVelocity (float acceleration, float maxSpeed)
		{
			IsIncreasingVelocity = true;

			while (CanIncreaseVelocity (maxSpeed))
			{
				if (!IsIncreasingVelocity)
					return;

				AddSpeedToTargetVelocity (acceleration);

				await Task.Yield();
			}

			if (!IsIncreasingVelocity)
				return;

			if (IsCurrentSpeedCloseToValue (maxSpeed))
			{
				SetTargetVelocity (maxSpeed);
			}

			IsIncreasingVelocity = false;
		}

		protected virtual bool CanIncreaseVelocity (float maxSpeed) =>
			physicalProperties.targetVelocity.magnitude < maxSpeed;

		protected async Task DecreaseTargetVelocity (float deceleration, float minSpeed = 0f)
		{
			IsDecreasingVelocity = true;

			while (CanDecreaseVelocity (minSpeed))
			{
				if (!IsDecreasingVelocity)
					return;

				SubtractSpeedFromTargetVelocity (deceleration);

				await Task.Yield();
			}

			if (!IsDecreasingVelocity)
				return;

			SetTargetVelocity (minSpeed);

			IsDecreasingVelocity = false;
		}

		protected virtual bool CanDecreaseVelocity (float minSpeed) =>
			physicalProperties.targetVelocity.magnitude > minSpeed + COMPARE_VELOCITY_TOLERANCE;

		protected virtual void AddSpeedToTargetVelocity (float speed)
		{
			physicalProperties.targetVelocity += physicalProperties.targetVelocityDirection * (speed * Time.deltaTime);
		}

		protected virtual void SubtractSpeedFromTargetVelocity (float speed)
		{
			physicalProperties.targetVelocity -= physicalProperties.targetVelocityDirection * (speed * Time.deltaTime);
		}

		protected virtual bool IsCurrentSpeedCloseToValue (float speed) =>
			Mathf.Round (physicalProperties.targetVelocity.magnitude) == speed;

		protected void ResetVelocityOnSwitchInputDirection()
		{
			if (!thirdPersonRotation.IsSameHorizontalDirection (physicalProperties.targetVelocity.x,
			                                                    inputSystem.InputHorizontalMovementDirection))
			{
				physicalProperties.targetVelocity = Vector3.zero;
			}
		}
	}
}