using BookCommunity.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookCommunity.Api.DTOs.ReadingList;

public class UpdateReadingListDto
{
    [Required]
    public ReadingStatus Status { get; set; }
}
