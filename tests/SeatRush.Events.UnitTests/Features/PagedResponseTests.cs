using SeatRush.Events.Application.Features.ListEvents;

namespace SeatRush.Events.UnitTests.Features;

public sealed class PagedResponseTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(20, 1)]
    [InlineData(21, 2)]
    public void TotalPages_rounds_up(int totalCount, int expectedPages)
    {
        var page = new PagedResponse<int>([], 1, 20, totalCount);

        Assert.Equal(expectedPages, page.TotalPages);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_page_size_that_is_not_positive_is_rejected(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedResponse<int>([], 1, pageSize, 5));
    }
}
