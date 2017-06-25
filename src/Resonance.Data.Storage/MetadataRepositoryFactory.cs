using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Resonance.Data.Storage
{
    public class MetadataRepositoryFactory : IMetadataRepositoryFactory
    {
        public IMetadataRepository Create(IMetadataRepositorySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            AssemblyName assemblyName = new AssemblyName(settings.AssemblyName);
            Assembly assembly = Assembly.Load(assemblyName);

            string typeName = settings.TypeName;

            if (settings.TypeName == null)
            {
                typeName = "MetadataRepository";
            }

            var type = assembly.DefinedTypes.FirstOrDefault(a => a.Name == typeName);

            if (type == null)
            {
                throw new Exception(string.Format("Unable to find type '{0}' in assembly '{1}'", settings.TypeName, settings.AssemblyName));
            }

            IMetadataRepository metadataRepository;

            if (settings.Parameters != null)
            {
                var parameters = new List<string>
                {
                    settings.ResonancePath
                };

                parameters.AddRange(settings.Parameters.Split(new[] { "::" }, StringSplitOptions.None).ToList());

                metadataRepository = Activator.CreateInstance(type.UnderlyingSystemType, parameters.ToArray()) as IMetadataRepository;
            }
            else
            {
                metadataRepository = Activator.CreateInstance(type.UnderlyingSystemType, settings.ResonancePath) as IMetadataRepository;
            }

            if (metadataRepository == null)
            {
                throw new Exception(string.Format("Unable to create instance of type '{0}' in assembly '{1}'", settings.TypeName, settings.AssemblyName));
            }

            return metadataRepository;
        }
    }
}