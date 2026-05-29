using System.Collections;
using System.ComponentModel;
using System.Globalization;
using static VE3NEA.NativeSoapySdr;

namespace VE3NEA
{
  //------------------------------------------------------------------------------------------------
  //                            SdrProperties, editable in PropertyGrid 
  //------------------------------------------------------------------------------------------------
  public class SdrProperties : List<SdrProperty>, ICustomTypeDescriptor
  {
    public SdrProperty? GetByName(string name)
    {
      // first found or null
      return Find(x => x.Name == name);
    }

    public AttributeCollection GetAttributes()
    {
      return TypeDescriptor.GetAttributes(this, true);
    }

    public string? GetClassName()
    {
      return TypeDescriptor.GetClassName(this, true);
    }

    public string? GetComponentName()
    {
      return TypeDescriptor.GetComponentName(this, true);
    }

    public TypeConverter? GetConverter()
    {
      return TypeDescriptor.GetConverter(this, true);
    }

    public EventDescriptor? GetDefaultEvent()
    {
      return TypeDescriptor.GetDefaultEvent(this, true);
    }

    public PropertyDescriptor? GetDefaultProperty()
    {
      return TypeDescriptor.GetDefaultProperty(this, true);
    }

    public object? GetEditor(Type editorBaseType)
    {
      return TypeDescriptor.GetEditor(this, editorBaseType, true);
    }

    public EventDescriptorCollection GetEvents()
    {
      return TypeDescriptor.GetEvents(this, true);
    }

    public EventDescriptorCollection GetEvents(Attribute[]? attributes)
    {
      return TypeDescriptor.GetEvents(this, attributes, true);
    }

    public PropertyDescriptorCollection GetProperties()
    {
      return TypeDescriptor.GetProperties(this, true);
    }

    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
      PropertyDescriptor[] newProps = new PropertyDescriptor[Count];
      for (int i = 0; i < Count; i++)
      {
        SdrProperty prop = this[i];
        newProps[i] = new SdrPropertyDescriptor(ref prop, attributes);
      }

      return new PropertyDescriptorCollection(newProps);
    }

    public object? GetPropertyOwner(PropertyDescriptor? pd)
    {
      return this;
    }
  }




  //------------------------------------------------------------------------------------------------
  //                                     SdrProperty
  //------------------------------------------------------------------------------------------------
  public class SdrProperty
  {
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Name => ArgInfo.Name;

    public string Value;
    public SoapySDRArgInfo ArgInfo;
    public string Category;

    public SdrProperty(SoapySDRArgInfo argInfo, string value, string category)
    {
      ArgInfo = argInfo;
      Value = value;
      Category = category;
    }
  }




  //------------------------------------------------------------------------------------------------
  //                                SdrPropertyDescriptor
  //------------------------------------------------------------------------------------------------
  public class SdrPropertyDescriptor : PropertyDescriptor
  {
    SdrProperty Property;

    public override string Category => Property.Category;


    public SdrPropertyDescriptor(ref SdrProperty property, Attribute[] attrs) : base(property.Name, attrs)
    {
      Property = property;
    }

    public override Type ComponentType => null;

    public override bool IsReadOnly => false;

    public override Type PropertyType => GetTypeFromArgInfo();

    public override bool CanResetValue(object component)
    {
      return false;
    }

    public override object? GetValue(object? component)
    {
      switch (Property.ArgInfo.Type)
      {
        case SoapySDRArgInfoType.Bool: return Property.Value == "true";
        case SoapySDRArgInfoType.Int: return int.Parse(Property.Value);
        case SoapySDRArgInfoType.Float: return float.Parse(Property.Value, CultureInfo.InvariantCulture);
        default: return OptionToName();
      }
    }

    private string OptionToName()
    {
      return Property.ArgInfo.Options.FirstOrDefault(x => x.Value == Property.Value).Key ?? Property.Value;
    }

    private string NameToOption(string name)
    {
      return Property.ArgInfo.Options.GetValueOrDefault(name) ?? name;
    }

    public override string Description => Property.ArgInfo.Description;

    public override void ResetValue(object component)
    {
      throw new NotImplementedException();
    }

    public override void SetValue(object? component, object? value)
    {
      switch (Property.ArgInfo.Type)
      {
        case SoapySDRArgInfoType.Bool: 
          Property.Value = value?.ToString()?.ToLower() ?? ""; 
          break;

        case SoapySDRArgInfoType.Int:
          Property.Value = NumericToString((int)value);
          break;
        case SoapySDRArgInfoType.Float:
          Property.Value = NumericToString((float)value);
          break;

        default:
          Property.Value = NameToOption((string)value);
          break;
      }
    }

    private string NumericToString(double value)
    {
      if (Property.ArgInfo.Range.minimum != 0 || Property.ArgInfo.Range.maximum != 0)
        value = Math.Max(Property.ArgInfo.Range.minimum, Math.Min(Property.ArgInfo.Range.maximum, value));        
      
      return value.ToString();
    }

    public override bool ShouldSerializeValue(object component)
    {
      return false;
    }


    private Type GetTypeFromArgInfo()
    {
      switch (Property.ArgInfo.Type)
      {
        case SoapySDRArgInfoType.Bool: return typeof(bool);
        case SoapySDRArgInfoType.Int: return typeof(int);
        case SoapySDRArgInfoType.Float: return typeof(float);
        default: return typeof(string);
      }
    }

    public override TypeConverter Converter => GetTypeConverter(); 

    private TypeConverter GetTypeConverter()
    {
      if (Property.ArgInfo.Type == SoapySDRArgInfoType.String && Property.ArgInfo.Options.Count > 0)
        return new OptionConverter(Property.ArgInfo);
      else 
        return base.Converter;
    }
  }




  //------------------------------------------------------------------------------------------------
  //                                   OptionConverter
  //------------------------------------------------------------------------------------------------
  public class OptionConverter : StringConverter
  {
    private readonly SoapySDRArgInfo argInfo;

    public OptionConverter(SoapySDRArgInfo argInfo)
    {
      this.argInfo = argInfo;
    }

    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
    {
      var coll = new StandardValuesCollection(argInfo.Options.Keys);
      return coll;
    }
  }
}
