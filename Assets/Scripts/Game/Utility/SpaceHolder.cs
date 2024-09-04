using DaggerfallWorkshop.Game.Serialization;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{
    public class SpaceHolder : MonoBehaviour
    {
        public DiscretizedSpace Space = null;
        public int RaycastBudgetPerFrame = 25;

        public readonly Vector3 Origin = Vector3.zero;
        public readonly Vector3 Step = new Vector3(0.7f, 0.7f, 0.7f);

        static SpaceHolder instance = null;
        public static SpaceHolder Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject();
                    go.name = "SpaceHolder";
                    instance = go.AddComponent<SpaceHolder>();
                }
                return instance;
            }
        }

        public DiscretizedSpace GetSpace() { return Space ?? (Space = BuildDiscretizedSpace()); }

        public void Start()
        {

        }

        protected DiscretizedSpace BuildDiscretizedSpace()
        {
            Space = new DiscretizedSpace(Origin, Step);
            Space.SetRaycastBudget(0);
            return Space;
        }

        public void OnEnable()
        {
            PlayerEnterExit.OnTransitionInterior += OnTransition;
            PlayerEnterExit.OnTransitionDungeonInterior += OnTransition;
            PlayerEnterExit.OnTransitionExterior += OnTransition;
            PlayerEnterExit.OnTransitionDungeonExterior += OnTransition;
            StartGameBehaviour.OnNewGame += OnNewGame;
            SaveLoadManager.OnLoad += OnLoad;
            StreamingWorld.OnFloatingOriginChange += OnFloatingOriginChange;
        }

        public void OnDisable()
        {
            PlayerEnterExit.OnTransitionInterior -= OnTransition;
            PlayerEnterExit.OnTransitionDungeonInterior -= OnTransition;
            PlayerEnterExit.OnTransitionExterior -= OnTransition;
            PlayerEnterExit.OnTransitionDungeonExterior -= OnTransition;
            StartGameBehaviour.OnNewGame -= OnNewGame;
            SaveLoadManager.OnLoad -= OnLoad;
            StreamingWorld.OnFloatingOriginChange -= OnFloatingOriginChange;
        }

        public void Update()
        {
            Space?.SetRaycastBudget(RaycastBudgetPerFrame);
        }

        private void OnTransition(PlayerEnterExit.TransitionEventArgs args)
        {
            Space = null;
        }

        private void OnLoad(SaveData_v1 saveData)
        {
            Space = null;
        }

        private void OnNewGame()
        {
            Space = null;
        }

        private void OnFloatingOriginChange()
        {
            Space = null;
        }
    }
}