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
    public class Module_02_INTv2 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // Your code goes here
            //View curView = doc.ActiveView;

            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.ViewType == ViewType.FloorPlan ||
                            v.ViewType == ViewType.CeilingPlan ||
                            v.ViewType == ViewType.Section ||
                            v.ViewType == ViewType.AreaPlan)
                .ToList();

            int totalTagCount = 0;
            foreach (var view in views)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc, view.Id);
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

                int viewTagCount = 0;
                using(Transaction t = new Transaction(doc))
                {
                    t.Start("Tag All Views");
                    foreach (Element curElem in collector)
                    {
                        // Get Location Point
                        XYZ instPoint;
                        LocationPoint locPoint;
                        LocationCurve locCurve;

                        Location curLoc = curElem.Location;

                        if (curLoc == null)
                            continue;
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

                            

                        ViewType curViewType = view.ViewType;

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

                                        //Check for Door Type under curtain wall
                                        foreach (ElementId subEleId in curWall.FindInserts(false, false, false, false))
                                        {
                                            Element subEle = doc.GetElement(subEleId);
                                            if (subEle is FamilyInstance && subEle.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors)
                                            {
                                                Reference subRef = new Reference(subEle);
                                                IndependentTag newTag2 = IndependentTag.Create(doc, doorTag.Id, view.Id, subRef, false, TagOrientation.Horizontal, instPoint);
                                            }
                                        }
                                    }

                                    else
                                    {
                                        curTagType = wallTag;
                                    }
                                }

                            Reference curRef = new Reference(curElem);

                            //Place Tag
                            IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, view.Id, curRef, false, TagOrientation.Horizontal, instPoint);
                            viewTagCount++;
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
                                IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, view.Id, curRef, false, TagOrientation.Horizontal, instPoint);
                                viewTagCount++;
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
                                IndependentTag newTag = IndependentTag.Create(doc, curTagType.Id, view.Id, curRef, false, TagOrientation.Horizontal, new XYZ(instPoint.X,instPoint.Y, instPoint.Z + 3));
                                viewTagCount++;
                            }
                        }

                        else if(curViewType == ViewType.AreaPlan)
                        {
                            //TaskDialog.Show("Test", "This is an area Plan!");

                            //Place Area Tag
                            if (curElem.Category.Name == "Areas")
                            {
                                ViewPlan curAreaPlan = view as ViewPlan;
                                Area curArea = curElem as Area;

                                AreaTag curAreaTag = doc.Create.NewAreaTag(curAreaPlan, curArea, new UV(instPoint.X, instPoint.Y));
                                curAreaTag.TagHeadPosition = new XYZ(instPoint.X, instPoint.Y,0);
                                curAreaTag.HasLeader = false;
                                viewTagCount++;
                            }
                        }

                    }

                    t.Commit();
                }
                totalTagCount += viewTagCount;
            }

            TaskDialog.Show("Tag Count", $"Total Tags placed in project{totalTagCount}");

            return Result.Succeeded;
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
