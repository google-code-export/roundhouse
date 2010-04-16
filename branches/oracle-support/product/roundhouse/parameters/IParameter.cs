namespace roundhouse.parameters
{
    public interface IParameter<T>
    {
        T underlying_type();
    }
}