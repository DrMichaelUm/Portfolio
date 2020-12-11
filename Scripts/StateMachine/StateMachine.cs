using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bimicore.BSG.StateMachinePattern
{
    /// <summary>
    /// Class to implement StateMachine pattern for any behaviour you want.
    /// </summary>
    public class StateMachine
    {
        private IState _currentState; //State that StateMachine operating at the moment

        //public bool isActive = true;

        /// <summary>
        /// Dictionary of all states and their transitions.
        /// </summary>
        private Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>();

        /// <summary>
        /// Transitions of the _currentState.
        /// </summary>
        private List<Transition> _currentTransitions = new List<Transition>();
        /// <summary>
        /// Transitions that can be done from any state. Basically this transition goes to state that can interrupt any other.
        /// </summary>
        private List<Transition> _anyTransitions = new List<Transition>();

        private static List<Transition> EmptyTransitions = new List<Transition>(0); //An empty list just for memory performance

        /// <summary>
        /// Class that represents transition to state with a particular condition.
        /// </summary>
        private class Transition
        {
            public Func<bool> Condition { get; }
            public IState To { get; }

            public Transition(IState to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }

        /// <summary>
        /// StateMachine "update" method. Constantly looking for available transitions from the current state and makes a transition by calling SetState function. 
        /// </summary>
        public void Tick()
        {
            var transition = GetTransition();
            if (transition != null)
            SetState(transition.To);

            _currentState?.Tick();
        }

        public void FixedTick()
        {
            _currentState?.FixedTick();
        }

        /// <summary>
        /// Makes a transition from current state to state mentioned as a parameter. If no states running - sets the initial state of StateMachine.
        /// </summary>
        /// <param name="state"> State, you want to transit to.</param>
        public void SetState(IState state)
        {
            if (state == _currentState)
                return;
            
            _currentState?.OnExit();
            _currentState = state;

            _transitions.TryGetValue(_currentState.GetType(), out _currentTransitions);
            if (_currentTransitions == null)
                _currentTransitions = EmptyTransitions;
            
            _currentState?.OnEnter();
        }

        /// <summary>
        ///  Add a transition between two states.
        /// </summary>
        /// <param name="from">State FROM which there is a transition goes.</param>
        /// <param name="to">State TO which there is a transition goes.</param>
        /// <param name="predicate">The condition function. Should return bool value.</param>
        public void AddTransition(IState from, IState to, Func<bool> predicate)
        {
            Type from_Type = from.GetType();
            
            if (_transitions.TryGetValue(from_Type, out var transitions) == false)
            {
                transitions = new List<Transition>();
                _transitions[from_Type] = transitions;
            }

            transitions.Add(new Transition(to, predicate));
        }

        /// <summary>
        ///  Add a transition to interrupting state.
        /// </summary>
        /// <param name="state">State that will interrupt any other state.</param>
        /// <param name="predicate">The condition function. Should return bool value.</param>
        public void AddAnyTransition(IState state, Func<bool> predicate)
        {
            _anyTransitions.Add(new Transition(state, predicate));
        }

        /// <summary>
        /// Get a condition-satisfying transition from the current state.
        /// </summary>
        /// <returns>StateMachine.Transition</returns>
        private Transition GetTransition()
        {
            int i = 0;
            while (i < _anyTransitions.Count)
            {
                if (_anyTransitions[i].Condition())
                    return _anyTransitions[i];
                i++;
            }

            while (i < _currentTransitions.Count)
            {
                if (_currentTransitions[i].Condition() && _currentTransitions[i].To.StateRestrictorsCount == 0)
                    return _currentTransitions[i];
                i++;
            }

            return null;
        }

        public void ExitCurrentState()
        {
            _currentState?.OnExit();
            _currentState = null;
        }
    }
}
