namespace TelmMed.Api.DTOs.Doctors
{
    public record ScheduleRequestDto(
    DateTime PreferredDate,
    TimeSpan PreferredTime,
    List<AlternativeSlotDto> Alternatives,
    string? Notes
);

    public record AlternativeSlotDto(DateTime Date, TimeSpan Time);
}
