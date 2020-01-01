// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2019 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Lypyl (lypyl@dfworkshop.net)
// Contributors:    TheLacus
// 
// Notes:
//

using UnityEngine;
using System;
using System.Collections.Generic;

namespace DaggerfallWorkshop.Game.Utility.ModSupport
{
    //loaded asset - used for lookups w/ mods
    public struct LoadedAsset
    {
        public Type T;
        public UnityEngine.Object Obj;

        public LoadedAsset(Type T, UnityEngine.Object Obj)
        {
            this.T = T;
            this.Obj = Obj;
        }
    }

    /// <summary>
    /// The content of a json mod manifest file, created from the Mod Builder and bundled with the mod itself.
    /// </summary>
    [Serializable]
    public class ModInfo
    {
        public string ModTitle;         //displayed in game
        public string ModVersion;
        public string ModAuthor;
        public string ContactInfo;
        public string DFUnity_Version;
        public string ModDescription;
        public string GUID = "invalid";
        public List<string> Files;      //list of assets to add to mod (only used during creation)

        /// <summary>
        /// Automatic asset injections defined by manifest .json file.
        /// These values are not available for edits from mods at runtime.
        /// </summary>
        [SerializeField]
        internal ModContributes Contributes;

        /// <summary>
        /// A list of mods that this mod depends on or is otherwise compatible with only if certain conditions are met.
        /// </summary>
        [SerializeField]
        internal ModDependency[] Dependencies;

        public ModInfo()
        {
            Files = new List<string>();
        }
    }

    /// <summary>
    /// Contributes provided by a mod to be injected in game.
    /// </summary>
    /// <remarks>
    /// The purpose of this section of the manifest file is to signal the presence of additional
    /// assets for the core game, which are imported automatically without the need of one-time
    /// scripts for every mod, or even scripting knowledge on the modder side.
    /// This class can be expanded over time as necessary but breaking changes should be avoided.
    /// <remarks/>
    [Serializable]
    internal sealed class ModContributes
    {
        /// <summary>
        /// Look-up maps that announce additional books to be imported.
        /// </summary>
        [SerializeField]
        internal string[] BooksMapping;

        /// <summary>
        /// Names of spell icon packs; each name corresponds to a <see cref="Texture2D"/>
        /// asset and a <see cref="TextAsset"/> with `.json` extension.
        /// </summary>
        [SerializeField]
        internal string[] SpellIcons;
    }

    /// <summary>
    /// A set of rules that defines the limits within which a mod is required or otherwise compatible with another one.
    /// </summary>
    /// <remarks>
    /// These are all the possible combinations:
    /// - Dependency: must be available, have higher priority and follow specified criteria.
    /// - Optional dependency: if is available it must have higher priority and follow specified criteria.
    /// - Peer dependency: must be available and follow specified criteria but higher priority is not required.
    /// - Optional peer dependency: if is available it must follow specified criteria but higher priority is not required.
    /// </summary>
    [Serializable]
    internal struct ModDependency
    {
        /// <summary>
        /// Name of target mod.
        /// </summary>
        [SerializeField]
        internal string Name;

        /// <summary>
        /// If true, target mod doesn't need to be available, but must validate these criteria if it is.
        /// </summary>
        [SerializeField]
        internal bool IsOptional;

        /// <summary>
        /// If true, target mod can be positioned anywhere in the load order, otherwise must be positioned above.
        /// </summary>
        [SerializeField]
        internal bool IsPeer;

        /// <summary>
        /// If not null this string is the minimum accepted version with format X.Y.Z.
        /// Pre-release identifiers following an hyphen are ignored in target version so they must be omitted here.
        /// For example "1.1.0" is higher than "1.0.12" and equal to "1.0.0-rc.1".
        /// </summary>
        [SerializeField]
        internal string Version;
    }

    /// <summary>
    /// Options for a mod setup intro point, meaning a method with the <see cref="Invoke"/> attribute.
    /// </summary>
    public struct SetupOptions : IComparable<SetupOptions>
    {
        /// <summary>
        /// The priority within invokable methods for the same mod.
        /// </summary>
        public readonly int priority;

        /// <summary>
        /// The mod that found target method inside its assemblies.
        /// </summary>
        public readonly Mod mod;

        /// <summary>
        /// The method to be invoked.
        /// </summary>
        public readonly System.Reflection.MethodInfo mi;

        public SetupOptions(int priority, Mod mod, System.Reflection.MethodInfo mi)
        {
            this.priority = priority;
            this.mod = mod;
            this.mi = mi;
        }

        /// <summary>
        /// Compares methods for their priority.
        /// </summary>
        public int CompareTo(SetupOptions other)
        {
            if (other.priority == priority)
                return 0;
            else if (this.priority < other.priority)
                return -1;
            return 1;
        }
    }

    /// <summary>
    /// Data passed to methods with the <see cref="Invoke"/> attribute when they are invoked by the Mod Manager.
    /// It contains informations required to initialize custom scripts provided by a mod,
    /// including the <see cref="Mod"/> instance associated to the class that receives this data.
    /// </summary>
    public struct InitParams
    {
        /// <summary>
        /// The title of the mod.
        /// </summary>
        public readonly string ModTitle;

        /// <summary>
        /// The position of the mod inside the mods collection.
        /// </summary>
        public readonly int ModIndex;
        
        /// <summary>
        /// The position of the mod in the load order.
        /// </summary>
        public readonly int LoadPriority;

        /// <summary>
        /// The total number of mods loaded by Mod Manager.
        /// </summary>
        public readonly int LoadedModsCount;

        /// <summary>
        /// The associated Mod instance that gives access, among the others, to bundled assets.
        /// </summary>
        public readonly Mod Mod;

        public InitParams(Mod Mod, int ModIndex, int LoadedModsCount)
        {
            this.Mod = Mod;
            this.ModTitle = Mod.Title;
            this.LoadPriority = Mod.LoadPriority;
            this.ModIndex = ModIndex;
            this.LoadedModsCount = LoadedModsCount;
        }
    }

    public struct Source
    {
        public TextAsset sourceTxt;
        public bool isPreCompiled;
    }

    /// <summary>
    /// Specify a non-generic, public, static, class method that only takes an <see cref="InitParams"/>
    /// struct for a parameter, to be called automatically by Mod Manager during mod setup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class Invoke : Attribute
    {
        public readonly int Priority;
        public readonly StateManager.StateTypes StartState;

        /// <summary>
        /// Request the mod manager to invoke this method at the specified state.
        /// </summary>
        /// <param name="startState">At which state the ModManager will invoke this method; typically this the Start or the Game state.</param>
        /// <param name="priority">Defines a per-mod order if there are multiple invoked methods for the same state.</param>
        public Invoke(StateManager.StateTypes startState = StateManager.StateTypes.Start, int priority = 99)
        {
            this.Priority = priority;
            this.StartState = startState;
        }
    }

    /// <summary>
    /// Takes part of load/save logic.
    /// </summary>
    public interface IHasModSaveData
    {
        /// <summary>
        /// The type of a custom class that holds save data and optionally use <see cref="FullSerializer.fsObjectAttribute"/> for versioning.
        /// </summary>
        Type SaveDataType { get; }

        /// <summary>
        /// Makes a new instance of <see cref="SaveDataType"/> with default values.
        /// </summary>
        object NewSaveData();

        /// <summary>
        /// Makes a new instance of <see cref="SaveDataType"/> for the current state or null if there is nothing to save.
        /// </summary>
        object GetSaveData();

        /// <summary>
        /// Restores retrieved data when a save is loaded.
        /// </summary>
        /// <param name="saveData">An instance of <see cref="SaveDataType"/>.</param>
        void RestoreSaveData(object saveData);
    }

    //used by mod builder window
    public enum ModCompressionOptions
    {
        LZ4 = 0,
        LZMA = 1,
        Uncompressed = 2,
    }

    public delegate void DFModMessageReceiver(string message, object data, DFModMessageCallback callBack);
    public delegate void DFModMessageCallback(string message, object data);
}
