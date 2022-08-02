﻿using System.Collections.Generic;
using System.Linq;
using Randomizer.Shared;

namespace Randomizer.SMZ3
{
    /// <summary>
    /// Represents a region in a game.
    /// </summary>
    public abstract class Region : IHasLocations
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Region"/> class for the
        /// specified world and configuration.
        /// </summary>
        /// <param name="world">The world the region is in.</param>
        /// <param name="config">The config used.</param>
        protected Region(World world, Config config)
        {
            Config = config;
            World = world;
        }

        /// <summary>
        /// Gets the name of the region.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the name of the overall area the region is a part of.
        /// </summary>
        public virtual string Area => Name;

        /// <summary>
        /// Gets a collection of alternate names for the region.
        /// </summary>
        public virtual IReadOnlyCollection<string> AlsoKnownAs { get; } = new List<string>();

        /// <summary>
        /// Gets a collection of every location in the region.
        /// </summary>
        public IEnumerable<Location> Locations => GetStandaloneLocations()
            .Concat(GetRooms().SelectMany(x => x.GetLocations()));

        /// <summary>
        /// Gets a collection of every room in the region.
        /// </summary>
        public IEnumerable<Room> Rooms => GetRooms();

        /// <summary>
        /// Gets the world the region is located in.
        /// </summary>
        public World World { get; }

        /// <summary>
        /// Gets the relative weight used to bias the randomization process.
        /// </summary>
        public int Weight { get; init; } = 0;

        /// <summary>
        /// Gets the randomizer configuration options.
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// The Logic to be used to determine if certain actions can be done
        /// </summary>
        public ILogic Logic => World.Logic;

        /// <summary>
        /// Gets the list of region-specific items, e.g. keys, maps, compasses.
        /// </summary>
        protected IList<ItemType> RegionItems { get; set; } = new List<ItemType>();

        /// <summary>
        /// Determines whether the specified item is specific to this region.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns>
        /// <see langword="true"/> if the item is specific to this region;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsRegionItem(Item item)
        {
            return RegionItems.Contains(item.Type);
        }

        /// <summary>
        /// Determines whether the specified item can be assigned to a location
        /// in this region.
        /// </summary>
        /// <param name="item">The item to fill.</param>
        /// <param name="items">The currently available items.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="item"/> can be
        /// assigned to a location in this region; otherwise, <see
        /// langword="false"/>.
        /// </returns>
        public virtual bool CanFill(Item item, Progression items)
        {
            return Config.Keysanity || !item.IsDungeonItem || IsRegionItem(item);
        }

        /// <summary>
        /// Returns a string that represents the region.
        /// </summary>
        /// <returns>A new string that represents the region.</returns>
        public override string ToString() => Name;

        /// <summary>
        /// Determines whether the region can be entered with the specified
        /// items.
        /// </summary>
        /// <param name="items">The currently available items.</param>
        /// <param name="requireRewards">If dungeon/boss rewards are required for the check</param>
        /// <returns>
        /// <see langword="true"/> if the region is available with <paramref
        /// name="items"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public virtual bool CanEnter(Progression items, bool requireRewards)
        {
            return true;
        }

        /// <summary>
        /// Returns a collection of all locations in this region that are not
        /// part of a room.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="Location"/> that do not exist in <see
        /// cref="Rooms"/>.
        /// </returns>
        public IEnumerable<Location> GetStandaloneLocations()
            => GetType().GetPropertyValues<Location>(this);

        protected IEnumerable<Room> GetRooms()
            => GetType().GetPropertyValues<Room>(this);
    }
}
