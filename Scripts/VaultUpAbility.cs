using Bimicore.BSG.StateMachinePattern;
using UnityEngine;

namespace Bimicore.BSG.ThirdPerson
{
	public class VaultUpAbility : MovementAbstractAbility, IState
	{
		public int StateRestrictorsCount { get; set; } = 0;

		[Header("Vault Properties")]
		[SerializeField] [Layer] public int vaultLayer = 0;


		[Header("Auto Attraction")] 
		[SerializeField] private float reachedTolerance = 0.35f;
		[SerializeField] private ParabolicAttraction vaultAttraction = null;
		[SerializeField] private bool debugPath = true;

		[Header("Collider Size Deformation")] 
		[SerializeField] private ColliderTransform minimizedCollider = ColliderTransform.zero;

		private float _currentTime = 0f;

		public void OnEnter()
		{
			CommonEnter();

			view.StartAnimation(animatorManager);

			_currentTime = 0f;
			InitializeAttractionToBorder();
			physicalProperties.ChangeColliderSettings(minimizedCollider);

			physicalProperties.ChangeCollisionState(vaultLayer, true);
		}

		public void Tick()
		{
			_currentTime += Time.deltaTime;
			CommonTick();
		}

		public void FixedTick()
		{
			SetTargetVelocity();
			CommonFixedTick();
		}

		public void OnExit()
		{
			physicalProperties.RestoreDefaultColliderHeight();
			physicalProperties.ChangeCollisionState(vaultLayer, false);
			CommonExit();
		}
		
		private void SetTargetVelocity()
		{
			physicalProperties.targetVelocity = vaultAttraction.GetCurrentVelocity(_currentTime);
		}

		private void InitializeAttractionToBorder ()
		{
			environmentScanner.TryGetBorderPoint(out var attractPointPos,
			                                     new Vector3(inputSystem.InputHorizontalMovementDirection,
			                                                 physicalProperties.targetVelocityDirection.y, 0), 5f);
			vaultAttraction.InitializeParabolicAttraction(attractPointPos);
		}
		
		public bool IsVaultReached() => vaultAttraction.IsPointReached(reachedTolerance);
		
		//TODO: resource-intensive debugging
		private void OnDrawGizmos()
		{
			if (debugPath && environmentScanner && environmentScanner.TryGetBorderPoint(out var possiblePoint, transform.forward, 5f))
			{
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(possiblePoint, 0.14f);
				Gizmos.color = Color.white;

				Gizmos.color = Color.gray;
				vaultAttraction.DrawPath(possiblePoint);
				Gizmos.color = Color.white;
			}
		}
	}
}