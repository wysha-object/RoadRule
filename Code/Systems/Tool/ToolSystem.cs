using System;
using System.Linq;
using Colossal.Entities;
using Game.Common;
using Game.Net;
using Game.Notifications;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI.Localization;
using Game.UI.Tooltip;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace RoadRule.Systems.Tool
{
    public partial class ToolSystem : ToolBaseSystem
    {
        public override string toolID => "RoadRule Tool";

        private UI.TooltipSystem m_TooltipSystem;

        private UI.UISystem m_UISystem;

        private StringTooltip m_AddEdgeTooltip;

        private StringTooltip m_RemoveEdgeTooltip;

        private Entity m_LastEntity;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TooltipSystem = World.GetOrCreateSystemManaged<UI.TooltipSystem>();
            m_UISystem = World.GetOrCreateSystemManaged<UI.UISystem>();
            m_ToolSystem.EventToolChanged += ToolChanged;

            m_AddEdgeTooltip = new StringTooltip
            {
                path = "RoadRule.AddEdge",
                icon = "Media/Mouse/LMB.svg",
                value = LocalizedString.Id("Tooltip.AddEdge"),
            };
            m_RemoveEdgeTooltip = new StringTooltip
            {
                path = "RoadRule.RemoveEdge",
                icon = "Media/Mouse/RMB.svg",
                value = LocalizedString.Id("Tooltip.RemoveEdge"),
            };
        }

        protected override void OnStartRunning()
        {
            applyAction.shouldBeEnabled = true;
            secondaryApplyAction.shouldBeEnabled = true;
            requireUnderground = false;
        }

        protected override void OnStopRunning()
        {
            applyAction.shouldBeEnabled = false;
            secondaryApplyAction.shouldBeEnabled = false;
        }

        protected override bool GetAllowApply()
        {
            return true;
        }

        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        private bool m_Underground;

        public override bool allowUnderground => true;

        public override void SetUnderground(bool isUnderground)
        {
            m_Underground = isUnderground;
        }

        public override void ElevationUp()
        {
            m_Underground = false;
        }

        public override void ElevationDown()
        {
            m_Underground = true;
        }

        public override void ElevationScroll()
        {
            m_Underground = !m_Underground;
        }

        public override void InitializeRaycast()
        {
            if (m_Underground)
            {
                m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
            }
            else
            {
                m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
            }
            m_ToolRaycastSystem.typeMask = TypeMask.Net;
            m_ToolRaycastSystem.raycastFlags = RaycastFlags.SubElements | RaycastFlags.Cargo | RaycastFlags.Passenger | RaycastFlags.EditorContainers;
            m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack | Layer.PublicTransportRoad | Layer.Pathway;
            m_ToolRaycastSystem.iconLayerMask = IconLayerMask.None;
            m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.None;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            requireUnderground = m_Underground;

            base.applyAction.shouldBeEnabled = true;
            base.secondaryApplyAction.shouldBeEnabled = true;
            bool raycastFlag = GetRaycastResult(out Entity entity, out RaycastHit hit);
            if (applyAction.WasReleasedThisFrame())
            {
                if (IsValidEntity(entity) && new UI.UISystem.ToolState[] { UI.UISystem.ToolState.Choosing, UI.UISystem.ToolState.Choosed }.Contains(m_UISystem.GetToolState()))
                {
                    m_UISystem.AddSelectedEdgeEntity(entity);
                }
            }
            if (secondaryApplyAction.WasReleasedThisFrame())
            {
                if (IsValidEntity(entity) && m_UISystem.GetToolState() == UI.UISystem.ToolState.Choosed)
                {
                    m_UISystem.RemoveSelectedEdgeEntity(entity);
                }
            }

            EntityManager.RemoveComponent<Highlighted>(m_LastEntity);
            EntityManager.AddComponent<BatchesUpdated>(m_LastEntity);
            if (IsValidEntity(entity))
            {
                EntityManager.AddComponent<Highlighted>(entity);
                EntityManager.AddComponent<BatchesUpdated>(entity);
            }
            m_LastEntity = entity;
            UpdateTooltip(entity);
            return inputDeps;
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
        {
            Mod.m_Log.Info($"Searching for traffic light prefab asset entities");
            EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<PlaceableNetData>());
            NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
            NativeArray<PlaceableNetData> placeableNetDataArray = query.ToComponentDataArray<PlaceableNetData>(Allocator.Temp);
            for (int i = 0; i < entityArray.Length; i++)
            {
                if ((placeableNetDataArray[i].m_SetUpgradeFlags.m_General & CompositionFlags.General.TrafficLights) == 0)
                {
                    continue;
                }
            }
        }

        private void UpdateTooltip(Entity entity)
        {
            m_TooltipSystem.m_TooltipList.Clear();
            if (IsValidEntity(entity))
            {
                if (new UI.UISystem.ToolState[] { UI.UISystem.ToolState.Choosing, UI.UISystem.ToolState.Choosed }.Contains(m_UISystem.GetToolState()))
                {
                    m_TooltipSystem.m_TooltipList.Add(m_AddEdgeTooltip);
                    m_TooltipSystem.m_TooltipList.Add(m_RemoveEdgeTooltip);
                }
            }
        }

        public bool IsValidEntity(Entity entity)
        {
            if (!EntityManager.HasComponent<Game.Net.Edge>(entity) || !EntityManager.HasBuffer<Game.Net.SubLane>(entity))
            {
                return false;
            }
            return true;
        }

        public void Enable()
        {
            m_ToolSystem.activeTool = this;
            m_TooltipSystem.m_TooltipList.Clear();
            m_TooltipSystem.Enabled = true;
        }

        public void Disable()
        {
            if (m_ToolSystem.activeTool == this)
            {
                m_ToolSystem.activeTool = m_DefaultToolSystem;
            }
            m_TooltipSystem.m_TooltipList.Clear();
            m_TooltipSystem.Enabled = false;
            EntityManager.RemoveComponent<Highlighted>(m_LastEntity);
            EntityManager.AddComponent<BatchesUpdated>(m_LastEntity);
        }

        private void ToolChanged(ToolBaseSystem system)
        {
            if (system != this)
            {
                m_UISystem.SetToolState(UI.UISystem.ToolState.Disabled);
            }
        }
    }
}
