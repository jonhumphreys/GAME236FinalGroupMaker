using System;

[System.Serializable]
public class Student
{
    public string Name;
    public int GroupNumber;

    public Student(string name, int groupNumber)
    {
        Name = name;
        GroupNumber = groupNumber;
    }

    public override string ToString()
    {
        return FormatStudentInfo();
    }

    private string FormatStudentInfo()
    {
        return $"{Name} (Group {GroupNumber})";
    }
}