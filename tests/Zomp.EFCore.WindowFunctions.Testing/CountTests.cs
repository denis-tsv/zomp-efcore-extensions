using static Zomp.EFCore.Testing.TestFixture;

namespace Zomp.EFCore.WindowFunctions.Testing;

public class CountTests
{
    private readonly TestDbContext dbContext;

    public CountTests(TestDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public void CountBasic()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(r.Id, EF.Functions.Over()),
        });

        var result = query.ToList();

        var count = TestRows.Length;
        var expectedSequence = Enumerable.Range(0, TestRows.Length).Select(_ => count);
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountBasicNullable()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(r.Col1, EF.Functions.Over()),
        });

        var result = query.ToList();

        var count = TestRows.Count(x => x.Col1 is not null);
        var expectedSequence = Enumerable.Range(0, TestRows.Length).Select(_ => count);
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountBetweenCurrentRowAndNext()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(r.Id, EF.Functions.Over().OrderBy(r.Id).Rows().FromCurrentRow().ToFollowing(1)),
        });

        var result = query.ToList();

        var expectedSequence = TestRows
            .Select((_, i) => i < TestRows.Length - 1 ? 2 : 1);
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountBetweenCurrentRowAndNextNullable()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(r.Col1, EF.Functions.Over().OrderBy(r.Id).Rows().FromCurrentRow().ToFollowing(1)),
        });

        var result = query.ToList();

        var expectedSequence = TestRows.Select((_, i)
            => TestRows.CountNonNulls(z => z.Col1, i, i + 1));
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountBetweenTwoPreceding()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(r.Col1, EF.Functions.Over().OrderBy(r.Id).Rows().FromPreceding(2).ToPreceding(1)),
        });

        var result = query.ToList();

        var expectedSequence = TestRows.Select((_, i)
            => TestRows.CountNonNulls(z => z.Col1, i - 2, i - 1));
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountBetweenTwoFollowing()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(r.Col1, EF.Functions.Over().OrderBy(r.Id).Rows().FromFollowing(1).ToFollowing(2)),
        });

        var result = query.ToList();

        var expectedSequence = TestRows.Select((_, i)
            => TestRows.CountNonNulls(z => z.Col1, i + 1, i + 2));
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountBetweenFollowingAndUnbounded()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Original = r,
            Count = EF.Functions.Count(r.Col1, EF.Functions.Over().OrderBy(r.Id).Rows().FromFollowing(1).ToUnbounded()),
        })
        .OrderBy(r => r.Original.Id);

        var result = query.ToList();

        var expectedSequence = TestRows.Select((_, i)
            => TestRows.CountNonNulls(z => z.Col1, i + 1, TestRows.Length - 1));
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountWithPartition()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(
                r.Col1,
                EF.Functions.Over().PartitionBy(r.Id / 10)),
        });

        var result = query.ToList();

        var groups = TestRows.GroupBy(r => r.Id / 10)
            .ToDictionary(
            r => r.Key,
            r => r.Count(z => z.Col1 is not null));

        var expectedSequence = TestRows.Select(r => groups[r.Id / 10]);
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountWith2Partitions()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Original = r,
            Count = EF.Functions.Count(
                r.Id,
                EF.Functions.Over().PartitionBy(r.Id / 10).ThenBy(r.Date.DayOfYear % 2)),
        })
        .OrderBy(r => r.Original.Id);

        var result = query.ToList();

        var groups = TestRows.GroupBy(z => (z.Id / 10, z.Date.DayOfYear % 2))
            .ToDictionary(
            r => r.Key,
            r => r.Count());

        var expectedSequence = TestRows.Select(r => groups[(r.Id / 10, r.Date.DayOfYear % 2)]);
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void SimpleCountWithCast()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count((long)r.Id, EF.Functions.Over()),
        });

        var result = query.ToList();

        var count = TestRows.Length;
        var expectedSequence = Enumerable.Range(0, TestRows.Length).Select(_ => (long)count);
        Assert.Equal(expectedSequence, result.Select(r => (long)r.Count));
    }

    public void CountWithCastToString()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(r.Col1.ToString(), EF.Functions.Over()),
        });

        var result = query.ToList();

        var count = TestRows.Count(r => r.Col1?.ToString(CultureInfo.InvariantCulture) != null);
        var expectedSequence = Enumerable.Range(0, TestRows.Length).Select(_ => count);
        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }

    public void CountBinary()
    {
        var query = dbContext.TestRows
        .Select(r => new
        {
            Count = EF.Functions.Count(r.IdBytes, EF.Functions.Over()),
        });

        var result = query.ToList();

        var count = TestRows.Count();
        var expectedSequence = Enumerable.Range(0, TestRows.Length).Select(_ => count);

        Assert.Equal(expectedSequence, result.Select(r => r.Count));
    }
}