using System;

#pragma warning disable CA1051
namespace MapXML.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class XMLParameterAttribute : Attribute
    {
        public readonly string AttributeName;
        /// <summary>
        /// Exact name of a function to be used as converter from string to the type of the parameter this attribute is applied to.
        /// The indicated function must be STATIC and must belong to the same class as the declaring method.
        /// The function must have <see cref="object"/> as return type and accept exactly 2 parameters: a <see cref="string"/> and an <see cref="IFormatProvider"/>
        /// <para/>
        /// Also see the delegate definition: <seealso cref="ConvertFromString"/>
        /// </summary>
        public readonly string? ConversionFunction;
        /// <summary>
        /// Exact name of a function to be used as converter from the parameter type to string.
        /// The indicated function must be STATIC and must belong to the same class as the declaring method.
        /// The function must have <see cref="string"/> as return type and accept exactly 2 parameters: a <see cref="object"/> and an <see cref="IFormatProvider"/>
        /// <para/>
        /// Also see the delegate definition: <seealso cref="ConvertFromString"/>
        /// </summary>
        public readonly string? ConversionBackFunction;

        public XMLParameterAttribute(string attributeName, string conversionFunction, string conversionBackFunction) : this(attributeName, conversionFunction)
        {
            this.ConversionBackFunction = conversionBackFunction;
        }
        public XMLParameterAttribute(string attributeName, string conversionFunction) : this(attributeName)
        {
            this.ConversionFunction = conversionFunction;
        }

        public XMLParameterAttribute(string attributeName)
        {
            AttributeName = attributeName;
        }
    }
}
