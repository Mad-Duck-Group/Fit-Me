namespace MadDuck.Scripts.Frameworks.StateMachine
{
    public abstract class State
    {
        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void Exit() { }
    }
    
    public abstract class StateMachine
    {
        private State _currentState;

        /// <summary>
        /// Changes the current state of the state machine.
        /// </summary>
        /// <param name="newState">New state to change to.</param>
        protected void ChangeState(State newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public void Update()
        {
            _currentState?.Update();
        }
    }
}
