﻿using System;
using Content.Client.GameObjects.Components.Actor;
using Content.Client.Input;
using Content.Client.Interfaces;
using Content.Client.Interfaces.Chat;
using Content.Client.Interfaces.Parallax;
using Content.Client.Parallax;
using Content.Client.Sandbox;
using Content.Client.State;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Cargo;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Gravity;
using Content.Shared.GameObjects.Components.Markers;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.GameObjects.Components.VendingMachines;
using Content.Shared.Kitchen;
using Robust.Client;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.State;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client
{
    public class EntryPoint : GameClient
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IBaseClient _baseClient;
        [Dependency] private readonly IEscapeMenuOwner _escapeMenuOwner;
        [Dependency] private readonly IGameController _gameController;
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IConfigurationManager _configurationManager;
#pragma warning restore 649

        public override void Init()
        {
            var factory = IoCManager.Resolve<IComponentFactory>();
            var prototypes = IoCManager.Resolve<IPrototypeManager>();

            factory.DoAutoRegistrations();

            var registerIgnore = new[]
            {
                "Anchorable",
                "AmmoBox",
                "Breakable",
                "Pickaxe",
                "Interactable",
                "Destructible",
                "Temperature",
                "PowerTransfer",
                "PowerNode",
                "PowerProvider",
                "PowerDevice",
                "PowerStorage",
                "PowerGenerator",
                "Explosive",
                "OnUseTimerTrigger",
                "ToolboxElectricalFill",
                "ToolboxEmergencyFill",
                "WarpPoint",
                "ToolboxGoldFill",
                "ToolLockerFill",
                "EmitSoundOnUse",
                "FootstepModifier",
                "HeatResistance",
                "Teleportable",
                "ItemTeleporter",
                "Portal",
                "EntityStorage",
                "PlaceableSurface",
                "Wirecutter",
                "Screwdriver",
                "Multitool",
                "Wrench",
                "Crowbar",
                "Projectile",
                "MeleeWeapon",
                "Storeable",
                "Dice",
                "Construction",
                "Apc",
                "Door",
                "PoweredLight",
                "Smes",
                "Powercell",
                "LightBulb",
                "Healing",
                "Catwalk",
                "RangedMagazine",
                "Ammo",
                "HitscanWeaponCapacitor",
                "PowerCell",
                "WeaponCapacitorCharger",
                "PowerCellCharger",
                "AiController",
                "PlayerInputMover",
                "Computer",
                "AsteroidRock",
                "ResearchServer",
                "ResearchPointSource",
                "ResearchClient",
                "IdCard",
                "Access",
                "AccessReader",
                "IdCardConsole",
                "Airlock",
                "MedicalScanner",
                "WirePlacer",
                "Species",
                "Drink",
                "Food",
                "FoodContainer",
                "Stomach",
                "Hunger",
                "Thirst",
                "Rotatable",
                "MagicMirror",
                "MedkitFill",
                "FloorTile",
                "FootstepSound",
                "UtilityBeltClothingFill",
                "ShuttleController",
                "HumanInventoryController",
                "UseDelay",
                "Pourable",
                "Paper",
                "Write",
                "Bloodstream",
                "TransformableContainer",
                "Mind",
                "MovementSpeedModifier",
                "StorageFill",
                "Mop",
                "Bucket",
                "Puddle",
                "CanSpill",
                "SpeedLoader",
                "Hitscan",
                "BoltActionBarrel",
                "PumpBarrel",
                "RevolverBarrel",
                "ExplosiveProjectile",
                "StunnableProjectile",
                "RandomPottedPlant",
                "CommunicationsConsole",
                "BarSign",
                "DroppedBodyPart",
                "DroppedMechanism",
                "BodyManager",
                "Stunnable",
                "SolarPanel",
                "BodyScanner",
                "Stunbaton",
                "EmergencyClosetFill",
                "Tool",
                "TilePrying",
                "RandomToolColor",
                "ConditionalSpawner",
                "PottedPlantHide",
                "SecureEntityStorage",
                "PresetIdCard",
                "SolarControlConsole",
                "BatteryBarrel",
                "FlashExplosive",
                "FlashProjectile",
                "Utensil",
            };

            foreach (var ignoreName in registerIgnore)
            {
                factory.RegisterIgnore(ignoreName);
            }

            factory.Register<SharedResearchConsoleComponent>();
            factory.Register<SharedLatheComponent>();
            factory.Register<SharedSpawnPointComponent>();

            factory.Register<SharedSolutionComponent>();

            factory.Register<SharedVendingMachineComponent>();
            factory.Register<SharedWiresComponent>();
            factory.Register<SharedCargoConsoleComponent>();
            factory.Register<SharedReagentDispenserComponent>();
            factory.Register<SharedMicrowaveComponent>();
            factory.Register<SharedGravityGeneratorComponent>();

            prototypes.RegisterIgnore("material");
            prototypes.RegisterIgnore("reaction"); //Chemical reactions only needed by server. Reactions checks are server-side.
            prototypes.RegisterIgnore("barSign");

            ClientContentIoC.Register();

            if (TestingCallbacks != null)
            {
                var cast = (ClientModuleTestingCallbacks) TestingCallbacks;
                cast.ClientBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();

            IoCManager.Resolve<IParallaxManager>().LoadParallax();
            IoCManager.Resolve<IBaseClient>().PlayerJoinedServer += SubscribePlayerAttachmentEvents;
            IoCManager.Resolve<IStylesheetManager>().Initialize();
            IoCManager.Resolve<IScreenshotHook>().Initialize();

            IoCManager.InjectDependencies(this);

            _escapeMenuOwner.Initialize();

            _baseClient.PlayerJoinedServer += (sender, args) =>
            {
                IoCManager.Resolve<IMapManager>().CreateNewMapEntity(MapId.Nullspace);
            };

             _configurationManager.RegisterCVar("outline.enabled", true);
        }

        /// <summary>
        /// Subscribe events to the player manager after the player manager is set up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void SubscribePlayerAttachmentEvents(object sender, EventArgs args)
        {
            _playerManager.LocalPlayer.EntityAttached += AttachPlayerToEntity;
            _playerManager.LocalPlayer.EntityDetached += DetachPlayerFromEntity;
        }

        /// <summary>
        /// Add the character interface master which combines all character interfaces into one window
        /// </summary>
        public static void AttachPlayerToEntity(EntityAttachedEventArgs eventArgs)
        {
            eventArgs.NewEntity.AddComponent<CharacterInterface>();
        }

        /// <summary>
        /// Remove the character interface master from this entity now that we have detached ourselves from it
        /// </summary>
        public static void DetachPlayerFromEntity(EntityDetachedEventArgs eventArgs)
        {
            eventArgs.OldEntity.RemoveComponent<CharacterInterface>();
        }

        public override void PostInit()
        {
            base.PostInit();

            // Setup key contexts
            var inputMan = IoCManager.Resolve<IInputManager>();
            ContentContexts.SetupContexts(inputMan.Contexts);

            IoCManager.Resolve<IGameHud>().Initialize();
            IoCManager.Resolve<IClientNotifyManager>().Initialize();
            IoCManager.Resolve<IClientGameTicker>().Initialize();
            IoCManager.Resolve<IOverlayManager>().AddOverlay(new ParallaxOverlay());
            IoCManager.Resolve<IChatManager>().Initialize();
            IoCManager.Resolve<ISandboxManager>().Initialize();
            IoCManager.Resolve<IClientPreferencesManager>().Initialize();

            _baseClient.RunLevelChanged += (sender, args) =>
            {
                if (args.NewLevel == ClientRunLevel.Initialize)
                {
                    SwitchToDefaultState(args.OldLevel == ClientRunLevel.Connected ||
                                         args.OldLevel == ClientRunLevel.InGame);
                }
            };

            SwitchToDefaultState();
        }

        private void SwitchToDefaultState(bool disconnected = false)
        {
            // Fire off into state dependent on launcher or not.

            if (_gameController.LaunchState.FromLauncher)
            {
                _stateManager.RequestStateChange<LauncherConnecting>();
                var state = (LauncherConnecting) _stateManager.CurrentState;

                if (disconnected)
                {
                    state.SetDisconnected();
                }
            }
            else
            {
                _stateManager.RequestStateChange<MainScreen>();
            }
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.FramePreEngine:
                    IoCManager.Resolve<IClientNotifyManager>().FrameUpdate(frameEventArgs);
                    IoCManager.Resolve<IChatManager>().FrameUpdate(frameEventArgs);
                    break;
            }
        }
    }
}
