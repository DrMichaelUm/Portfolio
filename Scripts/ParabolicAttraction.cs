using System;
using UnityEngine;

namespace Bimicore.BSG.ThirdPerson
{
	[Serializable]
	public class ParabolicAttraction
	{
		[SerializeField] private Transform obj = null;
		[SerializeField] private Vector3 attractPointPosition = Vector3.zero;
		[SerializeField] private float jumpHeight = 25f;
		[SerializeField] private float yAcceleration = -18f;
		[SerializeField] private float xAcceleration = 5f;
		[SerializeField] private float zAcceleration = 0f;
		[SerializeField] [Layer] public int objLayer = 0;
		[SerializeField] [Layer] public int attractObj = 0;

		public Vector3 AttractPointPosition
		{
			get => attractPointPosition;
			set => attractPointPosition = value;
		}

		private LaunchData _launchData;

		private readonly struct LaunchData
		{
			public readonly Vector3 initialVelocity;
			public readonly float timeToTarget;
			public readonly float angle;

			public LaunchData (Vector3 initialVelocity, float timeToTarget)
			{
				this.initialVelocity = initialVelocity;
				this.timeToTarget = timeToTarget;
				angle = Mathf.Acos(initialVelocity.x / initialVelocity.magnitude);
			}
		}

		public void InitializeParabolicAttraction()
		{
			_launchData = CalculateLaunchData();
		}

		// Current V(x,y) = (initV*cos(initAngle) + xAceleration*time, initV*sin(initAngle) + yAcceleration * time)
		public Vector3 GetCurrentVelocity (float currentTime) =>
			new Vector3(_launchData.initialVelocity.magnitude * Mathf.Cos(_launchData.angle) + 
			            Mathf.Sign(Mathf.Cos(_launchData.angle)) * xAcceleration * currentTime,
			            _launchData.initialVelocity.magnitude * Mathf.Sin(_launchData.angle) +
			            yAcceleration * currentTime,
			            _launchData.initialVelocity.z * currentTime);

		private LaunchData CalculateLaunchData()
		{
			var targetPosition = attractPointPosition;
			var characterPosition = obj.position;
			var displacementY = targetPosition.y - characterPosition.y;

			var displacementXZ = new Vector3(targetPosition.x - characterPosition.x, 0,
			                                 targetPosition.z - characterPosition.z);

			var time = Mathf.Sqrt(-2 * jumpHeight / yAcceleration) +
			           Mathf.Sqrt(2 * (displacementY - jumpHeight) / yAcceleration);
			
			var velocityY = Vector3.up * Mathf.Sqrt(-2 * yAcceleration * jumpHeight);

			var velocityXZ = (displacementXZ -
			                  new Vector3(0.5f * xAcceleration * time * time, 0,
			                              0.5f * zAcceleration * time * time)) / time;

			return new LaunchData(velocityXZ + velocityY * -Mathf.Sign(yAcceleration), time);
		}

		public void DrawPath()
		{
			
			var launchData = CalculateLaunchData();
			var previousDrawPoint = obj.position;

			var resolution = 30;

			for (var i = 1; i <= resolution; i++)
			{
				var simulationTime = i / (float) resolution * launchData.timeToTarget;

				var displacement = launchData.initialVelocity * simulationTime +
				                   Vector3.right * xAcceleration * simulationTime * simulationTime / 2f +
				                   Vector3.up * yAcceleration * simulationTime * simulationTime / 2f;
				var drawPoint = obj.position + displacement;
				Gizmos.DrawLine(previousDrawPoint, drawPoint);
				previousDrawPoint = drawPoint;
			}
		}

		public bool IsPointReached (float toleranceRegion)
		{
			if (Physics.Raycast(obj.position,
			                    attractPointPosition - obj.position,
			                    out var hit))
				if (Vector3.Distance(obj.position, attractPointPosition) < toleranceRegion)
					return true;

			return false;
		}

		public void ChangeCollisionState(bool isIgnored)
		{
			Physics.IgnoreLayerCollision(objLayer, attractObj, isIgnored);
		}
	}
}