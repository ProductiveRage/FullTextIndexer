using System;
using FullTextIndexer.Common.Lists;

namespace Tester.SourceData
{
	public sealed class Post
	{
		public Post(int id, NonBlankTrimmedString title, NonBlankTrimmedString body, NonBlankTrimmedString author, DateTime publishedAt, NonNullOrEmptyStringList tags)
		{
			if (title == null)
				throw new ArgumentNullException(nameof(title));
			if (body == null)
				throw new ArgumentNullException(nameof(body));
			if (author == null)
				throw new ArgumentNullException(nameof(author));
			if (tags == null)
				throw new ArgumentNullException(nameof(tags));

			Id = id;
			Title = title;
			Body = body;
			Author = author;
			PublishedAt = publishedAt;
			Tags = tags;
		}

		public int Id { get; }
		public NonBlankTrimmedString Title { get; }
		public NonBlankTrimmedString Body { get; }
		public NonBlankTrimmedString Author { get; }
		public DateTime PublishedAt { get; }
		public NonNullOrEmptyStringList Tags{ get; }
	}
}
