﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Resonance.Data.Storage
{
    public class MetadataRepositoryFactory : IMetadataRepositoryFactory
    {
        private readonly IMetadataRepositorySettings _settings;

        public MetadataRepositoryFactory(IMetadataRepositorySettings settings)
        {
            _settings = settings;
        }

        public IMetadataRepository CreateMetadataRepository()
        {
            var assemblyName = new AssemblyName(_settings.AssemblyName);
            var assembly = Assembly.Load(assemblyName);

            var typeName = _settings.TypeName;

            if (_settings.TypeName == null)
            {
                typeName = "MetadataRepository";
            }

            var type = assembly.DefinedTypes.FirstOrDefault(a => a.Name == typeName);

            if (type == null)
            {
                throw new Exception($"Unable to find type '{_settings.TypeName}' in assembly '{_settings.AssemblyName}'");
            }

            IMetadataRepository metadataRepository;

            if (_settings.Parameters != null)
            {
                var parameters = new List<string>
                {
                    _settings.ResonancePath
                };

                parameters.AddRange(_settings.Parameters.Split(new[] { "::" }, StringSplitOptions.None).ToList());

                metadataRepository = Activator.CreateInstance(type.UnderlyingSystemType, parameters.ToArray()) as IMetadataRepository;
            }
            else
            {
                metadataRepository = Activator.CreateInstance(type.UnderlyingSystemType, _settings.ResonancePath) as IMetadataRepository;
            }

            if (metadataRepository == null)
            {
                throw new Exception($"Unable to create instance of type '{_settings.TypeName}' in assembly '{_settings.AssemblyName}'");
            }

            metadataRepository.ConfigureAsync().GetAwaiter().GetResult();

            return metadataRepository;
        }
    }
}