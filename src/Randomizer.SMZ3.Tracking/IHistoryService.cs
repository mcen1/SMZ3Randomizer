﻿using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using Microsoft.Extensions.Logging;
using Randomizer.Data.WorldData;
using Randomizer.Shared.Enums;
using Randomizer.Shared.Models;
using Randomizer.Data.Configuration.ConfigTypes;

namespace Randomizer.SMZ3.Tracking.VoiceCommands
{
    /// <summary>
    /// Service for managing the history of events through a playthrough
    /// </summary>
    public interface IHistoryService
    {
        /// <summary>
        /// Sets the tracker to be used for getting the elapsed time
        /// </summary>
        /// <param name="tracker"></param>
        public void SetTracker(Tracker tracker);

        /// <summary>
        /// Adds an event to the history log
        /// </summary>
        /// <param name="type">The type of event</param>
        /// <param name="isImportant">If this is an important event or not</param>
        /// <param name="objectName">The name of the event being logged</param>
        /// <param name="location">The optional location of where this event happened</param>
        /// <returns>The created event</returns>
        public TrackerHistoryEvent AddEvent(HistoryEventType type, bool isImportant, string objectName, Location? location = null);

        /// <summary>
        /// Adds an event to the history log
        /// </summary>
        /// <param name="histEvent">The event to add</param>
        public void AddEvent(TrackerHistoryEvent histEvent);

        /// <summary>
        /// Removes the event that was added last to the log
        /// </summary>
        public void RemoveLastEvent();

        /// <summary>
        /// Removes a specific event from the log
        /// </summary>
        /// <param name="histEvent">The event to log</param>
        public void Remove(TrackerHistoryEvent histEvent);

        /// <summary>
        /// Retrieves the current history log
        /// </summary>
        /// <returns>The collection of events</returns>
        public IReadOnlyCollection<TrackerHistoryEvent> GetHistory();

        /// <summary>
        /// Creates the progression log based off of the history
        /// </summary>
        /// <param name="rom">The rom that the history is for</param>
        /// <param name="history">All of the events to log</param>
        /// <param name="importantOnly">If only important events should be logged or not</param>
        /// <returns>The generated log text</returns>
        public string GenerateHistoryText(GeneratedRom rom, IReadOnlyCollection<TrackerHistoryEvent> history, bool importantOnly = true);
    }
}
