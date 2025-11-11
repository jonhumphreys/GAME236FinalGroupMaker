using System.Collections.Generic;

[System.Serializable]
public class StudentGroup
{
    public int GroupNumber;
    public List<Student> Students;

    public StudentGroup(int groupNumber)
    {
        GroupNumber = groupNumber;
        Students = new List<Student>();
    }

    public void AddStudent(Student student)
    {
        if (IsValidStudent(student))
        {
            Students.Add(student);
        }
    }

    public int StudentCount
    {
        get { return GetStudentCount(); }
    }

    public override string ToString()
    {
        return FormatGroupInfo();
    }

    private bool IsValidStudent(Student student)
    {
        return student != null;
    }

    private int GetStudentCount()
    {
        return Students.Count;
    }

    private string FormatGroupInfo()
    {
        return $"Group {GroupNumber}: {StudentCount} students";
    }
}