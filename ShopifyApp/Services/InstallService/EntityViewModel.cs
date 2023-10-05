using ShopifyApp.Api.ExigoWebservice;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ShopifyApp.Services
{

    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "")]
    public class EntityViewModel
    {
        public EntityViewModel()
        {

        }
        public EntityViewModel(string type)
        {
            Filename = $"~/App_Data/Entities/{type}.entity";
        }

        [XmlElement(ElementName = "EntityName")]
        public string EntityName { get; set; }

        [XmlElement(ElementName = "EntitySetName")]
        public string EntitySetName { get; set; }

        [XmlElement(ElementName = "SchemaName")]
        public string SchemaName { get; set; }

        [XmlElement(ElementName = "SyncTypeID")]
        public int SyncTypeID { get; set; }

        [XmlElement(ElementName = "IsLog")]
        public string IsLog { get; set; }

        [XmlElement(ElementName = "MaxLogDays")]
        public string MaxLogDays { get; set; }

        public PropertyViewModel[] Properties { get; set; }
        public string Filename { get; set; }
        public string XML
        {
            get
            {
                var path = HttpContext.Current.Server.MapPath(Filename);
                var xmlInputData = File.ReadAllText(path);
                return xmlInputData;
            }
        }
        public Entity Get()
        {
            var entityViewModel = Deserialize<EntityViewModel>(XML);
            var properties = new List<Property>();
            foreach(var property in entityViewModel.Properties)
            {
                var prop = new Property
                {
                    Name = property.Name,
                    IsKey = property.IsKey,
                    IsNew = property.IsNew,
                    IsAutoNumber = property.IsAutoNumber,
                    AllowDbNull = property.AllowDbNull,
                    Type = property.Type,
                    DefaultName = property.DefaultName,
                    DefaultValue = property.DefaultValue,
                    Size = property.Size
                };
                properties.Add(prop);
            }
            Entity newEntity = new Entity
            {
                EntityName = entityViewModel.EntityName,
                EntitySetName = entityViewModel.EntitySetName,
                SchemaName = entityViewModel.SchemaName,
                SyncTypeID = entityViewModel.SyncTypeID,
                Properties = properties.ToArray()
            };
            return newEntity;
        }
        private T Deserialize<T>(string input) where T : class
        {
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (StringReader sr = new StringReader(input))
            {
                return (T)ser.Deserialize(sr);
            }
        }

        private string Serialize<T>(T ObjectToSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(ObjectToSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, ObjectToSerialize);
                return textWriter.ToString();
            }
        }
        
    }
    public class PropertyViewModel : Property
    {
    }
}