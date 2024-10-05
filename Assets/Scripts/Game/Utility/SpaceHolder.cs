using System;
using DaggerfallWorkshop.Game.Serialization;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{
    public class SpaceHolder : MonoBehaviour
    {
        public DiscretizedNavigableSpace Space = null;

        public readonly Vector3 Origin = Vector3.zero;
        // Use Spherecasts on a grid, should be sufficient to pass thru gridResolution + 2 * Radius openings?
        // At least along axes
        public static readonly float GridResolution = 0.65f;
        public static readonly float Radius = 0.2f;

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

        public DiscretizedNavigableSpace GetSpace() { return Space ?? (Space = BuildDiscretizedSpace()); }

        public void Start()
        {

        }

        protected DiscretizedNavigableSpace BuildDiscretizedSpace()
        {
            Vector3 Step = new Vector3(GridResolution, GridResolution, GridResolution);
            Space = new DiscretizedNavigableSpace(Origin, Step, Radius);
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

        public void FixedUpdate()
        {
            if (DaggerfallUnity.Settings.HearingMaxCycles > 0)
            {
                DiscretizedNavigableSpace.SetCyclesBudget(DaggerfallUnity.Settings.HearingMaxCycles);
            }
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