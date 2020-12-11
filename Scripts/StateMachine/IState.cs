namespace Bimicore.BSG.StateMachinePattern
{
	/// <summary>
	/// Interface for StateMachine's states
	/// </summary>
	public interface IState
	{
		/// <summary>
		/// Can this state be entered now. If not, the number indicates how many scripts forbid it
		/// </summary>
		int StateRestrictorsCount { get; set; }

		/// <summary>
		/// Executed at the entrance to the state
		/// </summary>
		void OnEnter();

		/// <summary>
		/// Will be performed throughout the state
		/// </summary>
		void Tick();

		/// <summary>
		/// Will be performed in every frame of FixedUpdate of current state
		/// </summary>
		void FixedTick();

		/// <summary>
		/// Executed on exit from the state
		/// </summary>
		void OnExit();
	}
}
