namespace Assets.ShaderUtils
{
    public class ShaderUniform<T>
    {
        private readonly ShaderUniformName _name;
        private readonly T _value;
        private readonly string _directName;

        public ShaderUniform(ShaderUniformName name, T value)
        {
            _name = name;
            _value = value;
        }

        public ShaderUniform(string directName, T value)
        {
            _directName = directName;
            _value = value;
        }

        public string Name
        {
            get
            {
                if (_directName != null)
                {
                    return _directName;
                }
                else
                {
                    return _name.ToString();
                }
            }
        }

        public T Get()
        {
            return _value;
        }
    }
}