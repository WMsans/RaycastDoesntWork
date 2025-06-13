using System;

namespace sapra.InfiniteLands{
    public enum State{Idle, WaitingDependencies, Processing, Done}
    public class NodeState{
        public State state{get; private set;} = State.Idle;
        public Action OnCompleted;
        public bool completed => state == State.Done;
        public void SetState(State state){
            this.state = state;
            if(state == State.Done)
                OnCompleted?.Invoke();
        }
        public void Reuse(){
            state = State.Idle;
        }
    }
}