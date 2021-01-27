using System;
using Newtonsoft.Json.Linq;

namespace Elements.Serialization.JSON
{
    /// <summary>
    /// A JSON object migration.
    /// A migration mutates the JSON before deserialization.
    /// An object may be migrated multiple times in the process of deserialization.
    /// Migrations for an object are identified by the object's discriminator,
    /// and applied in order according to the from->to properties. 
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        /// The version being migrated to.
        /// </summary>
        Version From { get; }

        /// <summary>
        /// The version being migrated from.
        /// </summary>
        Version To { get; }

        /// <summary>
        /// The JSON path expression to return JObjects of the type.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Migrate the specified object forward.
        /// </summary>
        /// <param name="token">A JToken.</param>
        void Migrate(JToken token);
    }
}