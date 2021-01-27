using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Elements.Serialization.JSON
{
    /// <summary>
    /// An object which processes ordered migrations of JSON.
    /// </summary>
    public class Migrator
    {
        private List<IMigration> _migrations;
        private static Migrator _instance;

        /// <summary>
        /// The migrator singelton.
        /// </summary>
        public static Migrator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Migrator();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Construct a migrator.
        /// </summary>
        private Migrator()
        {
            var migrationTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.IsClass && typeof(IMigration).IsAssignableFrom(t))).ToList();
            _migrations = new List<IMigration>();
            foreach (var t in migrationTypes)
            {
                _migrations.Add((IMigration)Activator.CreateInstance(t));
            }
        }

        /// <summary>
        /// Migrate the JObject.
        /// </summary>
        /// <param name="model">The object to migrate.</param>
        /// <param name="errors">A collection of migration errors.</param>
        public void Migrate(JObject model, out List<string> errors)
        {
            // Assume version <=0.8.1 for models with models with no version specified.
            var incomingVersion = model.ContainsKey("ElementsVersion") ? model["ElementsVersion"].ToObject<Version>() : new Version(0, 8, 1);
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            errors = new List<string>();
            if (incomingVersion == currentVersion)
            {
                // Do nothing.
                return;
            }

            if (incomingVersion > currentVersion)
            {
                errors.Add($"Backwards migration from version {incomingVersion} to version {currentVersion} is not supported.");
                return;
            }

            var discriminatorGroups = _migrations.GroupBy(m => m.Path);
            foreach (var group in discriminatorGroups)
            {
                var tokens = model.SelectTokens(group.Key).ToList();
                var orderedMigrations = group.Where(g => g.From >= incomingVersion && g.To <= currentVersion);

                foreach (var migration in orderedMigrations)
                {
                    foreach (var token in tokens)
                    {
                        migration.Migrate(token);
                    }
                }
            }
        }
    }
}