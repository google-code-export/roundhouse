using System.Data;

namespace roundhouse.parameters
{
    class AdoNetParameter : IParameter<IDbDataParameter>
    {
        private readonly IDbDataParameter parameter;

        public AdoNetParameter(IDbDataParameter parameter)
        {
            this.parameter = parameter;
        }

        public IDbDataParameter underlying_type()
        {
            return parameter;
        }
    }
}