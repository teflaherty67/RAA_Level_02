#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

#endregion

namespace RAA_Level_02
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // step 1: not used

            // step 2: open form

            frmProjectSetup curForm= new frmProjectSetup()
            {
                Width = 500,
                Height = 360,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                Topmost = true,
            };

            if(curForm.ShowDialog() == false)
            {
                return Result.Cancelled;
            }

            // step 3: in the code-behind

            // step 4: get form data and do something

            string filePath = curForm.GetFilePath();

            if(filePath == null )
            {
                return Result.Cancelled;
            }

            // read csv file

            string[] levelArray = System.IO.File.ReadAllLines(filePath);

            //read csv data into a list

            List<string[]> levelData = new List<string[]>();

            foreach( string level in levelArray ) 
            {
                string[] cellData = level.Split(',');
                levelData.Add(cellData);
            }

            // remove header row

            levelData.RemoveAt(0);

            Transaction t = new Transaction(doc);
            t.Start("Project Setup");

            // create levels & views

            foreach( string[] curLevelData in levelData )
            {
                // set variables

                string levelName = curLevelData[0];
                string elevImperialString = curLevelData[1];
                string elevMetricString = curLevelData[2];

                double levelElevation = 0;

                if(curForm.GetUnits() == "Imperial")
                {
                    // imperial

                    double.TryParse(elevImperialString, out levelElevation);
                }
                else
                {
                    // metric

                    double metricConvert = 0;
                    double.TryParse(elevMetricString, out metricConvert);
                    levelElevation = metricConvert * 3.28084;
                }

                // create levels

                Level curLevel = Level.Create(doc, levelElevation);
                curLevel.Name = levelName;

                // create views

                if( curForm.CreateFloorPlan() == true )
                {
                    ViewFamilyType floorPlanVFT = GetViewFamilyTypeByName(doc, "Floor Plan", ViewFamily.FloorPlan);

                    ViewPlan curFloorPlan = ViewPlan.Create(doc, floorPlanVFT.Id, curLevel.Id);
                }

                if(curForm.CreateCeilingPlan()  == true )
                {
                    ViewFamilyType ceilingPlanVFT = GetViewFamilyTypeByName(doc, "Ceiling Plan", ViewFamily.CeilingPlan);

                    ViewPlan curCeilingPlan = ViewPlan.Create(doc, ceilingPlanVFT.Id, curLevel.Id);
                }
            }

            t.Commit();
            t.Dispose();

            TaskDialog.Show("Complete", "Created " + levelData.Count + " levels and views.");

            return Result.Succeeded;
        }

        private ViewFamilyType GetViewFamilyTypeByName(Document doc, string vftName, ViewFamily viewFamily)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(ViewFamilyType));

            foreach(ViewFamilyType curVFT in collector)
            {
                if(curVFT.Name == vftName && curVFT.ViewFamily == viewFamily)
                    return curVFT;
            }

            return null;
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
