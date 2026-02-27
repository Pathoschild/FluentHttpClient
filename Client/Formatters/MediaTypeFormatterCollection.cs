using System.Collections.ObjectModel;
using System.Linq;

namespace Pathoschild.Http.Client.Formatters;

/// <summary>A collection of <see cref="IMediaTypeFormatter"/> instances.</summary>
public class MediaTypeFormatterCollection : Collection<IMediaTypeFormatter>
{
    /// <summary>Get the first formatter which supports the given media type.</summary>
    /// <param name="mediaType">The media type.</param>
    public IMediaTypeFormatter? FindReader(string mediaType)
    {
        return this.FirstOrDefault(f => f.SupportedMediaTypes.Any(m => m == mediaType) && f.CanReadType(typeof(object)));
    }

    /// <summary>Get the first formatter which supports the given media type.</summary>
    /// <param name="mediaType">The media type.</param>
    public IMediaTypeFormatter? FindWriter(string mediaType)
    {
        return this.FirstOrDefault(f => f.SupportedMediaTypes.Any(m => m == mediaType) && f.CanWriteType(typeof(object)));
    }
}
