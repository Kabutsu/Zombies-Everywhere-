using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class DataLog {
    private static StreamWriter writer;
    private const string FILENAME = "log.txt";

    public static void CreateNewLog()
    {
        try
        {
            writer = new StreamWriter(FILENAME, false);
            writer.WriteLine("GAME STARTED");
            writer.Close();
        }
        catch (DirectoryNotFoundException ex)
        {
            Debug.Log(ex.StackTrace);
        }
        catch (FileNotFoundException ex)
        {
            Debug.Log(ex.StackTrace);
        }
        catch (FileLoadException ex)
        {
            Debug.Log(ex.StackTrace);
        }
        catch (IOException ex)
        {
            Debug.Log(ex.StackTrace);
        }
    }

	public static void Print(string message)
    {
        try
        {
            writer = new StreamWriter(FILENAME, true);
            writer.WriteLine(message);
            writer.Close();
        } catch (DirectoryNotFoundException ex)
        {
            Debug.Log(ex.StackTrace);
        } catch (FileNotFoundException ex)
        {
            Debug.Log(ex.StackTrace);
        } catch (FileLoadException ex)
        {
            Debug.Log(ex.StackTrace);
        } catch (IOException ex)
        {
            Debug.Log(ex.StackTrace);
        }
    }

}
