// DTOs/ProjectResponseDto.cs
using ProjectManager.DTOs;
public class ProjectResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; }
    public string CreatorId { get; set; }
    public string CreatorName { get; set; }


    // List of Project Members (Navigation property)
    public List<ProjectMemberDto> Members { get; set; }
}