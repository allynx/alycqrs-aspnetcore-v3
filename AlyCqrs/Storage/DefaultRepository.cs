using AlyCqrs.Domains;
using AlyCqrs.Domains.Sources;
using AlyCqrs.Events;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlyCqrs.Storage
{
    public class DefaultRepository<T> : IRepository<T> where T : AggregateRoot,new()
    {
        private readonly IEventBus _eventBus;
        private readonly IEventStorage _storage;
        private readonly IMemoryCache _memoryCache;

        public DefaultRepository(IMemoryCache memoryCache, IEventBus eventBus, IEventStorage storage)
        {
            _eventBus = eventBus;
            _storage = storage;
            _memoryCache = memoryCache;
        }

        public async Task<T> GetByKeyAsync(Guid aggregateKey)
        {

            if (!_memoryCache.TryGetValue<T>(aggregateKey, out T aggregate))
            {
                Memento memento = await _storage.GetMementoAsync(aggregateKey);
                aggregate = JsonSerializer.Deserialize<T>(memento.AggregateBinary);

                IEnumerable<Event> events = await _storage.GetEventsAsync(aggregateKey, memento.Version);
                foreach (Event e in events.OrderBy(o => o.Version))
                {
                    aggregate.ReplayEvent(e);
                }
            }

            return aggregate;
        }

        public async Task SaveAsync(AggregateRoot aggregate)
        {
            Type type = aggregate.GetType();
            string aggregateTypeName = type.FullName;
            Queue<Event> events = aggregate.GetUnCommittedEvent();
            if (aggregate.Version % 5 == 1)
            {
                byte[] buffer = JsonSerializer.SerializeToUtf8Bytes(aggregate);
                await _storage.SaveAsync(aggregate.Id, aggregate.Version, aggregateTypeName, buffer, events);
            }
            else
            {
                await _storage.SaveAsync(aggregate.Id, aggregate.Version, aggregateTypeName, events);
            }

            _memoryCache.Set(aggregate.Id, aggregate);
        }
    }
}
