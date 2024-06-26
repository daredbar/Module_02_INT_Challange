#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

#endregion

namespace Module_02_INT_Challange
{
    [Transaction(TransactionMode.Manual)]
    public class Module_02_INT : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Your code goes here
            View curView = doc.ActiveView;
            FilteredElementCollector collector = new FilteredElementCollector(doc, curView.Id);

            List<BuiltInCategory> catList = new List<BuiltInCategory>();
            catList.Add(BuiltInCategory.OST_Areas);
            catList.Add(BuiltInCategory.OST_Walls);
            catList.Add(BuiltInCategory.OST_Doors);
            catList.Add(BuiltInCategory.OST_Furniture);
            catList.Add(BuiltInCategory.OST_LightingFixtures);
            catList.Add(BuiltInCategory.OST_Rooms);
            catList.Add(BuiltInCategory.OST_Windows);

            ElementMulticategoryFilter catFilter = new ElementMulticategoryFilter(catList);
            collector.WherePasses(catFilter).WhereElementIsNotElementType();

            // Use LINQ
            FamilySymbol wallTag = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName.Equals("M_Wall Tag"))
                .First();

            FamilySymbol curtainWallTag = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName.Equals("M_Curtain Wall Tag"))
                .First();

            FamilySymbol doorTag = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName.Equals("M_Door Tag"))
                .First();

            FamilySymbol furnTag = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName.Equals("M_Furniture Tag"))
                .First();

            FamilySymbol lightFixTag = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName.Equals("M_Lighting Fixture Tag"))
                .First();

            FamilySymbol roomTag = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
               .Cast<FamilySymbol>()
                .Where(x => x.FamilyName.Equals("M_Room Tag"))
                .First();

            FamilySymbol windowTag = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x => x.FamilyName.Equals("M_Window Tag"))
                .First();

            // Dictionary
            Dictionary<string, FamilySymbol> tags = new Dictionary<string, FamilySymbol>();
            tags.Add("Walls", wallTag);
            tags.Add("Doors", doorTag);
            tags.Add("Furniture", furnTag);
            tags.Add("Lighting Fixtures", lightFixTag);
            tags.Add("Rooms", roomTag);
            tags.Add("Windows", windowTag);

            int tagCount = 0;

            using(Transaction t = new Transaction(doc))
            {
                t.Start("Tag All");
                foreach (Element curElem in collector)
                {
                    // Get Location Point
                    XYZ instPoint;
                    LocationPoint locPoint;
                    LocationCurve locCurve;

                    Location curLoc = curElem.Location;

                    if (curLoc == null)
                    {
                        //Use Bounding box for Curtain Doors
                        if (curElem.Category.Name == "Doors")
                        {
                            BoundingBoxXYZ bBox = curElem.get_BoundingBox(curView);
                            if (bBox != null)
                            {
                                instPoint = (bBox.Min + bBox.Max) / 2;
                            }
                            else
                                continue;
                            
                        }
                        else
                            continue;
                    }
                    else
                    {
                        locPoint = curLoc as LocationPoint;

                        if (curElem.Category.Name == "Windows")
                        {
                            instPoint = locPoint.Point;
                            instPoint = new XYZ(instPoint.X, instPoint.Y + 3, instPoint.Z);
                        }

                        else
                        {
                            if (locPoint != null)
                            {
                                instPoint = locPoint.Point;
                            }
                            else
                            {
                                locCurve = curLoc as LocationCurve;
                                Curve curCurve = locCurve.Curve;

                                instPoint = Utils.GetMidPointBetweenTwoPoints(curCurve.GetEndPoint(0), curCurve.GetEndPoint(1));
                            }
                        }

                    }


                    ViewType curViewType = curView.ViewType;

                    if (curViewType == ViewType.FloorPlan)
                    {
                        List<string> floorPlanCat = new List<string>
                        {"Walls","Doors","Furniture","Rooms","Windows"};

                        if (floorPlanCat.Contains(curElem.Category.Name))
                        {
                        FamilySymbol curTagType = tags[curElem.Category.Name];
                        //Check Wall Type
                            if (curElem.Category.Name == "Walls")
                            {
                            Wall curWall = curElem as Wall;
                            WallType curWallTpe = curWall.WallType;

                                if (curWallTpe.Kind == WallKind.Curtain)
                                {
                                    curTagType = curtainWallTag;
                                }

                                else
                                {
                                    curTagType = wallTag;
                                }
                            }

                        Reference curRef = new Reference(curElem);

                        //Place Tag
                        IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, curView.Id, curRef, false, TagOrientation.Horizontal, instPoint);
                        tagCount++;
                        }
                    }

                    else if (curViewType == ViewType.CeilingPlan)
                    {
                        List<string> ceilingPlanCat = new List<string>
                        { "Rooms", "Lighting Fixtures"};
                        if (ceilingPlanCat.Contains(curElem.Category.Name))
                        {
                            FamilySymbol curTagType = tags[curElem.Category.Name];

                            Reference curRef = new Reference(curElem);

                            //Place Tag
                            IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, curView.Id, curRef, false, TagOrientation.Horizontal, instPoint);
                            tagCount++;
                        }
                    }

                    else if (curViewType == ViewType.Section)
                    {
                        List<string> sectionCat = new List<string>
                        { "Rooms"};
                        if (sectionCat.Contains(curElem.Category.Name))
                        {
                            FamilySymbol curTagType = tags[curElem.Category.Name];

                            Reference curRef = new Reference(curElem);

                            //Place Tag
                            IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, curView.Id, curRef, false, TagOrientation.Horizontal, new XYZ(instPoint.X,instPoint.Y, instPoint.Z + 3));
                            tagCount++;
                        }
                    }

                    else if(curViewType == ViewType.AreaPlan)
                    {
                        //TaskDialog.Show("Test", "This is an area Plan!");

                        //Place Area Tag
                        if (curElem.Category.Name == "Areas")
                        {
                            ViewPlan curAreaPlan = curView as ViewPlan;
                            Area curArea = curElem as Area;

                            AreaTag curAreaTag = doc.Create.NewAreaTag(curAreaPlan, curArea, new UV(instPoint.X, instPoint.Y));
                            curAreaTag.TagHeadPosition = new XYZ(instPoint.X, instPoint.Y,0);
                            curAreaTag.HasLeader = false;
                            tagCount++;
                        }
                    }

                }

                t.Commit();
            }
            TaskDialog.Show("Tag Count", $"Total Tags places {tagCount}");

            return Result.Succeeded;
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
