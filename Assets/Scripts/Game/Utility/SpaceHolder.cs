using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{
    public class SpaceHolder : MonoBehaviour
    {
        public DiscretizedSpace Space = null;
        public int RaycastBudgetPerFrame = 50;

        public readonly Vector3 Origin = Vector3.zero;
        public readonly Vector3 Step = new Vector3(1f, 1f, 1f);

        public DiscretizedSpace GetSpace() { return Space ?? Space = BuildDiscretizedSpace(); }

        public void Start()
        {

        }

        protected DiscretizedSpace BuildDiscretizedSpace()
        {
            space = new DiscretizedSpace(Origin, Step);
            space.SetRaycastBudget(0);
            return space;
        }

        public void OnEnable()
        {
            PlayerEnterExit.OnTransitionInterior += DiscardSpace;
            PlayerEnterExit.OnTransitionDungeonInterior += DiscardSpace;
            PlayerEnterExit.OnTransitionExterior += DiscardSpace;
            PlayerEnterExit.OnTransitionDungeonExterior += DiscardSpace;
            StartGameBehaviour.OnNewGame += DiscardSpace;
            SaveLoadManager.OnLoad += DiscardSpace;
            StreamingWorld.OnFloatingOriginChange += DiscardSpace;
        }

        public void OnDisable()
        {
            PlayerEnterExit.OnTransitionInterior -= DiscardSpace;
            PlayerEnterExit.OnTransitionDungeonInterior -= DiscardSpace;
            PlayerEnterExit.OnTransitionExterior -= DiscardSpace;
            PlayerEnterExit.OnTransitionDungeonExterior -= DiscardSpace;
            StartGameBehaviour.OnNewGame -= DiscardSpace;
            SaveLoadManager.OnLoad -= DiscardSpace;
            StreamingWorld.OnFloatingOriginChange -= DiscardSpace;
        }

        public void Update()
        {
            space?.SetRaycastBudget(RaycastBudgetPerFrame);
        }

        private void DiscardSpace(PlayerEnterExit.TransitionEventArgs args)
        {
            space = null;
        }
    }
}