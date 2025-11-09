using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class StudentDataLoader
{
    private const int ExpectedColumnCount = 2;
    private const int StudentNameIndex = 0;
    private const int GroupNumberIndex = 1;

    public static List<StudentGroup> LoadFromCSV(string filePath)
    {
        Dictionary<int, StudentGroup> groupDict = new Dictionary<int, StudentGroup>();

        if (!ValidateFilePath(filePath))
        {
            return CreateEmptyGroupList();
        }

        try
        {
            string[] lines = ReadAllLines(filePath);
            ProcessAllLines(lines, groupDict);
            LogLoadResults(lines.Length, groupDict.Count);
        }
        catch (Exception e)
        {
            LogLoadError(e);
        }

        return ConvertToSortedList(groupDict);
    }

    private static bool ValidateFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            LogFileNotFound(filePath);
            return false;
        }
        return true;
    }

    private static List<StudentGroup> CreateEmptyGroupList()
    {
        return new List<StudentGroup>();
    }

    private static string[] ReadAllLines(string filePath)
    {
        return File.ReadAllLines(filePath);
    }

    private static void ProcessAllLines(string[] lines, Dictionary<int, StudentGroup> groupDict)
    {
        foreach (string line in lines)
        {
            ProcessSingleLine(line, groupDict);
        }
    }

    private static void ProcessSingleLine(string line, Dictionary<int, StudentGroup> groupDict)
    {
        if (ShouldSkipLine(line))
        {
            return;
        }

        string[] parts = ParseLine(line);

        if (!IsValidLineFormat(parts, line))
        {
            return;
        }

        if (TryExtractStudentData(parts, out string studentName, out int groupNumber))
        {
            AddStudentToGroup(studentName, groupNumber, groupDict);
        }
    }

    private static bool ShouldSkipLine(string line)
    {
        return string.IsNullOrWhiteSpace(line);
    }

    private static string[] ParseLine(string line)
    {
        return line.Split(',');
    }

    private static bool IsValidLineFormat(string[] parts, string line)
    {
        if (parts.Length != ExpectedColumnCount)
        {
            LogInvalidLineFormat(line);
            return false;
        }
        return true;
    }

    private static bool TryExtractStudentData(string[] parts, out string studentName, out int groupNumber)
    {
        studentName = ExtractStudentName(parts);
        return TryParseGroupNumber(parts, studentName, out groupNumber);
    }

    private static string ExtractStudentName(string[] parts)
    {
        return parts[StudentNameIndex].Trim();
    }

    private static bool TryParseGroupNumber(string[] parts, string studentName, out int groupNumber)
    {
        if (!int.TryParse(parts[GroupNumberIndex].Trim(), out groupNumber))
        {
            LogInvalidGroupNumber(studentName);
            return false;
        }
        return true;
    }

    private static void AddStudentToGroup(string studentName, int groupNumber, Dictionary<int, StudentGroup> groupDict)
    {
        Student student = CreateStudent(studentName, groupNumber);
        EnsureGroupExists(groupNumber, groupDict);
        AddStudentToGroupDictionary(student, groupNumber, groupDict);
    }

    private static Student CreateStudent(string studentName, int groupNumber)
    {
        return new Student(studentName, groupNumber);
    }

    private static void EnsureGroupExists(int groupNumber, Dictionary<int, StudentGroup> groupDict)
    {
        if (!groupDict.ContainsKey(groupNumber))
        {
            groupDict[groupNumber] = CreateNewGroup(groupNumber);
        }
    }

    private static StudentGroup CreateNewGroup(int groupNumber)
    {
        return new StudentGroup(groupNumber);
    }

    private static void AddStudentToGroupDictionary(Student student, int groupNumber, Dictionary<int, StudentGroup> groupDict)
    {
        groupDict[groupNumber].AddStudent(student);
    }

    private static List<StudentGroup> ConvertToSortedList(Dictionary<int, StudentGroup> groupDict)
    {
        return groupDict
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .ToList();
    }

    private static void LogFileNotFound(string filePath)
    {
        Debug.LogError($"File not found: {filePath}");
    }

    private static void LogInvalidLineFormat(string line)
    {
        Debug.LogWarning($"Invalid line format: {line}");
    }

    private static void LogInvalidGroupNumber(string studentName)
    {
        Debug.LogWarning($"Invalid group number for student: {studentName}");
    }

    private static void LogLoadResults(int lineCount, int groupCount)
    {
        Debug.Log($"Loaded {lineCount} students into {groupCount} groups");
    }

    private static void LogLoadError(Exception e)
    {
        Debug.LogError($"Error loading student data: {e.Message}");
    }
}