﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Trivial.Console
{
    /// <summary>
    /// The verb handler.
    /// It can be used for sub-application.
    /// </summary>
    public abstract class Verb
    {
        /// <summary>
        /// The cancllation token source.
        /// </summary>
        private CancellationTokenSource cancel = new CancellationTokenSource();

        /// <summary>
        /// Gets the descripiton of the verb handler.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// The input arguments.
        /// </summary>
        public Arguments Arguments { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether it is cancelled.
        /// </summary>
        public bool IsCancelled
        {
            get
            {
                return cancel.IsCancellationRequested;
            }
        }

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        public CancellationToken CancellationToken
        {
            get
            {
                return cancel.Token;
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="dispatcher">The caller.</param>
        public virtual void Init(Dispatcher dispatcher)
        {
        }

        /// <summary>
        /// Processes.
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// Gets a value indicating whether the input parameter or the current environment is valid.
        /// </summary>
        /// <returns>true if the input parameter or the current environment is valid; otherwise, false.</returns>
        public virtual bool IsValid()
        {
            return true;
        }

        /// <summary>
        /// Communicates a request for cancellation.
        /// </summary>
        public virtual void Cancel()
        {
            cancel.Cancel();
        }

        /// <summary>
        /// Gets details help message.
        /// </summary>
        /// <returns>The string of the usage documentation content.</returns>
        public virtual string GetHelp()
        {
            return null;
        }
    }

    /// <summary>
    /// The asynchronized verb handler.
    /// </summary>
    public abstract class AsyncVerb: Verb
    {
        /// <summary>
        /// Processes.
        /// </summary>
        public override void Process()
        {
            var task = ProcessAsync();
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                if (task.IsCanceled) ProcessCancelled(ex.InnerException as TaskCanceledException);
                else ProcessFailed(ex);
            }
        }

        /// <summary>
        /// Processes.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        protected abstract Task ProcessAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Processes.
        /// </summary>
        /// <returns>The task.</returns>
        public Task ProcessAsync()
        {
            return ProcessAsync(CancellationToken);
        }

        /// <summary>
        /// Occurs when it is cancelled.
        /// </summary>
        /// <param name="ex">The task cancelled exception.</param>
        public virtual void ProcessCancelled(TaskCanceledException ex)
        {
        }

        /// <summary>
        /// Occurs when it is failed to process.
        /// </summary>
        /// <param name="ex">The exception. Its inner exceptions property contains information about the exception or exceptions.</param>
        public virtual void ProcessFailed(AggregateException ex)
        {
        }
    }

    /// <summary>
    /// Help verb handler.
    /// </summary>
    public class HelpVerb : Verb
    {
        /// <summary>
        /// The inner messages store.
        /// </summary>
        internal class Item
        {
            /// <summary>
            /// Initializes a new instance of the HelpVerb.Item class.
            /// </summary>
            /// <param name="key">The verb key.</param>
            /// <param name="value">The verb description.</param>
            public Item(string key, string value)
            {
                Key = key;
                Value = value;
            }

            /// <summary>
            /// Gets the verb key.
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// Gets the verb description.
            /// </summary>
            public string Value { get; }
        }

        /// <summary>
        /// The default verb handler message.
        /// </summary>
        private string defaultUsage;

        /// <summary>
        /// The handler messages of all other verbs.
        /// </summary>
        private List<Item> items = new List<Item>();

        /// <summary>
        /// Gets or sets the description message.
        /// </summary>
        public string DescriptionMessage { get; set; } = "Get help.";

        /// <summary>
        /// Gets the descripiton of the verb handler.
        /// </summary>
        public override string Description
        {
            get
            {
                return DescriptionMessage;
            }
        }

        /// <summary>
        /// Gets the additional description which will be appended to the last.
        /// </summary>
        public string FurtherDescription { get; set; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="dispatcher">The caller.</param>
        public override void Init(Dispatcher dispatcher)
        {
            base.Init(dispatcher);
            var defaultVerb = dispatcher.DefaultVerb;
            if (defaultVerb != null) defaultUsage = defaultVerb.Description;
            foreach (var item in dispatcher.VerbsRegistered())
            {
                if (string.IsNullOrWhiteSpace(item.MatchDescription)) continue;
                items.Add(new Item(item.MatchDescription, item.Verb.Description));
            }
        }

        /// <summary>
        /// Processes.
        /// </summary>
        public override void Process()
        {
            Utilities.WriteLine(defaultUsage);
            foreach (var item in items)
            {
                Utilities.WriteLine(item.Key);
                if (item.Value != null) Utilities.WriteLine(item.Value.Replace("{0}", item.Key));
            }

            Utilities.WriteLine(FurtherDescription);
        }
    }

    /// <summary>
    /// Base exit verb handler.
    /// </summary>
    public abstract class BaseExitVerb : Verb
    {
    }

    /// <summary>
    /// Exit verb handler.
    /// </summary>
    public class ExitVerb : BaseExitVerb
    {
        /// <summary>
        /// Gets or sets a value indicating whether it is only for turning back parent dispatcher.
        /// </summary>
        public bool Back { get; set; }

        /// <summary>
        /// Gets or sets the description string for back.
        /// </summary>
        public string BackMessage { get; set; } = "Close the current conversation.";

        /// <summary>
        /// Gets or sets the description string for exit.
        /// </summary>
        public string ExitMessage { get; set; } = "Exit this application.";

        /// <summary>
        /// Gets or sets the exit string.
        /// </summary>
        public string ByeMessage { get; set; } = "Bye!";

        /// <summary>
        /// Gets the descripiton of the verb handler.
        /// </summary>
        public override string Description
        {
            get
            {
                return Back ? BackMessage : ExitMessage;
            }
        }

        /// <summary>
        /// Processes.
        /// </summary>
        public override void Process()
        {
            if (!Back) Utilities.WriteLine(ByeMessage);
        }
    }
}
