using System;
using System.Collections.Generic;
using Elements.Geometry.Solids;
using Elements.Validators;

namespace Elements.Geometry
{
    public partial class Representation
    {
        internal override int SortPriority => 2;

        /// <summary>
        /// Construct a representation.
        /// </summary>
        /// <param name="solidOperations">A collection of solid operations.</param>
        public Representation(IList<SolidOperation> @solidOperations)
            : base(Guid.NewGuid(), null)
        {
            var validator = Validator.Instance.GetFirstValidatorForType<Representation>();
            if (validator != null)
            {
                validator.PreConstruct(new object[] { @solidOperations, this.Id, this.Name });
            }

            this.SolidOperations = @solidOperations;

            if (validator != null)
            {
                validator.PostConstruct(this);
            }
        }
    }
}