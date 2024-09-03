using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Project1.Revit.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project1.Revit.IsoPipeDimension {
  public class IsoDimensionUpdater : IUpdater {
    readonly UpdaterId _UpdaterId;
    private string _TextNoteIdParam = "Dimension_TextNote_ID";
    private string _TargetPipeIdParam = "Target_Pipe_ID";

    public IsoDimensionUpdater(AddInId id) {
      var guid = Guid.Parse("4BD92E02-ABF9-4F9C-AE77-9F6EA69C3245");
      _UpdaterId = new UpdaterId(id, guid);
    }

    public void Execute(UpdaterData data) {
      var doc = data.GetDocument();
      var modElems = data.GetModifiedElementIds();
      var addElems = data.GetAddedElementIds();
      var delElems = data.GetDeletedElementIds();

      ElementId elemId = ElementId.InvalidElementId;
      if (modElems.Count != 0) {
        elemId = modElems.FirstOrDefault();
      }
      else if (addElems.Count != 0) {
        elemId = addElems.FirstOrDefault();
      }
      if (elemId != ElementId.InvalidElementId) {
        var param = doc.GetElement(elemId).LookupParameter(_TextNoteIdParam);
        if (param == null) { return; }
      }


      if (addElems.Count != 0 && (addElems.Count == delElems.Count)) {
        ExtensionDimesionTextNote(doc, addElems);
      }
      if (delElems.Count != 0 && (addElems.Count != delElems.Count)) {
        DeleteDimsionTextNote(doc);
      }
      if (modElems.Count != 0) {
        UpdateDimension(doc, modElems);
      }
    }

    public string GetAdditionalInformation() {
      return string.Empty;
    }

    public ChangePriority GetChangePriority() {
      return ChangePriority.FreeStandingComponents;
    }

    public UpdaterId GetUpdaterId() {
      return _UpdaterId;
    }

    public string GetUpdaterName() {
      return typeof(IsoDimensionUpdater).Name;
    }


    private void UpdateDimension(Document doc,
        ICollection<ElementId> elemIds) {
      var elem = doc.GetElement(elemIds.FirstOrDefault());
      if (elem.LookupParameter(_TextNoteIdParam) == null) { return; }
      var lengthParam = elem.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
      var forgeId = lengthParam.GetUnitTypeId();

      using (var tx = new SubTransaction(doc)) {
        try {
          tx.Start();

          foreach (var elemId in elemIds) {
            var pipe = (Pipe)doc.GetElement(elemId);
            //파이프 커넥터 XYZ 및 엘보의 센터 XYZ 가져오기
            //var points = ReferencePoints(pipe);
            var points = PipeDimensionHelper.GetPipeDimensionPoints(pipe);

            if (points == null || points.Count < 2) {
              continue;
            }
            var dimension = UnitUtils.ConvertFromInternalUnits(
              points[0].DistanceTo(points[1]), forgeId);
            dimension = MathUtils.RoundDoubleToInt(dimension);

            var textNoteId = new ElementId(
                pipe.LookupParameter(_TextNoteIdParam).AsInteger());

            var textNote = doc.GetElement(textNoteId) as TextNote;
            if (textNote == null) { continue; }

            textNote.Text = dimension.ToString();
            var centerPnt = (points[0] + points[1]) / 2;
            ElementTransformUtils.MoveElement(doc, textNoteId,
                centerPnt - textNote.Coord);
          }

          tx.Commit();
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => ISO Dimension Updater SubTransaction");
        }
      }
    }

    /// <summary>
    /// 배관에 배관을 연장해서 그릴 시 배관이 삭제 후 생성됨
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="elemIds"></param>
    private void ExtensionDimesionTextNote(Document doc,
        ICollection<ElementId> elemIds) {
      var tempElem = doc.GetElement(elemIds.FirstOrDefault());
      if (tempElem.LookupParameter(_TextNoteIdParam) == null) { return; }
      var unitTypeId = tempElem.get_Parameter(
          BuiltInParameter.CURVE_ELEM_LENGTH)?.GetUnitTypeId();

      var deleteTextNoteIds = new List<ElementId>();
      TextNote deleteText = null;
      var textnotes = doc.CategoryElements(BuiltInCategory.OST_TextNotes);
      foreach (TextNote textnote in textnotes) {
        var param = textnote.LookupParameter(_TargetPipeIdParam);
        if (param == null) { continue; }
        var pipeId = new ElementId(param.AsInteger());
        if (doc.GetElement(pipeId) == null) {
          deleteText = textnote;
          deleteTextNoteIds.Add(textnote.Id);
        }
      }

      if (deleteText == null) { return; }
      var view3D = (View3D)doc.GetElement(deleteText.OwnerViewId);
      var orientation = view3D.GetOrientation();

      // 작업평면 설정
      Plane plane = Plane.CreateByNormalAndOrigin(
          orientation.ForwardDirection, orientation.EyePosition);

      // TextNoteType 가져오기
      var defaultText = doc.GetDefaultElementTypeId(
          ElementTypeGroup.TextNoteType);
      var textNoteType = doc.GetElement(defaultText) as TextNoteType;
      textNoteType.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).Set(1);

      using (var tx = new SubTransaction(doc)) {
        try {
          tx.Start();

          foreach (var elemId in elemIds) {
            var element = doc.GetElement(elemId);

            //파이프 커넥터 XYZ 및 엘보의 센터 XYZ 가져오기
            List<XYZ> points = null;
            if (element is Pipe pipe) {
              points = PipeDimensionHelper.GetPipeDimensionPoints(pipe);
            }
            else if (element is FamilyInstance familyInstance) {
              points = GetElbowToFittingDimensionPoints(familyInstance);
            }

            var position = (points[0] + points[1]) / 2;

            // 배관 vector의 Rotation값
            var dist0 = plane.GetDistancePlaneToPoint(points[0]);
            var dist1 = plane.GetDistancePlaneToPoint(points[1]);
            var planePoint0 = points[0] - (dist0 * orientation.ForwardDirection);
            var planePoint1 = points[1] - (dist1 * orientation.ForwardDirection);

            var vector0 = planePoint0 - planePoint1;
            var rotation = vector0.AngleOnPlaneTo(view3D.RightDirection,
                orientation.ForwardDirection);

            var dimension = UnitUtils.ConvertFromInternalUnits(
                points[0].DistanceTo(points[1]), unitTypeId);
            dimension = MathUtils.RoundDoubleToInt(dimension);

            // 치수 문자 작성
            var textOptions = new TextNoteOptions {
              TypeId = textNoteType.Id,
              HorizontalAlignment = HorizontalTextAlignment.Center,
              VerticalAlignment = VerticalTextAlignment.Middle,
              Rotation = rotation,
              KeepRotatedTextReadable = true
            };
            var textNote = TextNote.Create(doc, view3D.Id,
                position, dimension.ToString(), textOptions);
            var pipeParma = element.LookupParameter(_TextNoteIdParam);
            pipeParma.Set(textNote.Id.IntegerValue);
            var textnoteParam = textNote.LookupParameter(_TargetPipeIdParam);
            textnoteParam.Set(element.Id.IntegerValue);
          }

          if (deleteTextNoteIds.Any()) { doc.Delete(deleteTextNoteIds); }

          tx.Commit();
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => ISO Dimension Updater SubTransaction");
        }
      }
    }

    /// <summary>
    /// Element가 삭제 된 후 Updater가 실행됨
    /// Element를 읽을 수 없음
    /// TextNote에 설정한 파라미터 값을 읽어 해당 배관이 없을 시 삭제
    /// </summary>
    /// <param name="doc"></param>
    private void DeleteDimsionTextNote(Document doc) {
      var deleteTextNoteIds = new List<ElementId>();

      var textnotes = doc.CategoryElements(BuiltInCategory.OST_TextNotes);
      foreach (var textnote in textnotes) {
        var param = textnote.LookupParameter(_TargetPipeIdParam);
        if (param == null) { continue; }
        var pipeId = new ElementId(param.AsInteger());
        if (doc.GetElement(pipeId) == null) {
          deleteTextNoteIds.Add(textnote.Id);
        }
      }

      using (var tx = new SubTransaction(doc)) {
        try {
          tx.Start();

          doc.Delete(deleteTextNoteIds);

          tx.Commit();
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => ISO Dimension Delete SubTransaction");
        }
      }
    }

    private List<XYZ> ReferencePoints(Pipe pipe) {
      var points = new List<XYZ>();

      var connect = pipe.ConnectorManager.Connectors;
      foreach (Connector item in connect) {
        if (!item.IsConnected) {
          points.Add(item.Origin);
        }
        else {
          foreach (Connector conn in item.AllRefs) {
            var owner = conn.Owner;
            if (owner.Id == pipe.Id) { continue; }
            if (conn.Domain == Domain.DomainUndefined) { continue; }
            if (owner.GetPartType() == PartType.Elbow) {
              points.Add(owner.GetLocationPoint());
              continue;
            }
            points.Add(item.Origin);
          }
        }
      }

      return points;
    }

    private List<XYZ> GetElbowToFittingDimensionPoints(FamilyInstance elbow) {
      var points = new List<XYZ>();
      var fitting = elbow.GetMechanicalFitting();
      if (elbow.GetPartType() == PartType.Elbow) {
        points.Add(elbow.GetLocationPoint());

        var connectors = fitting.ConnectorManager.Connectors;
        foreach (Connector connector in connectors) {
          var part = connector.GetConnectedPart();
          if (part.GetMechanicalFitting() != null) {
            points.Add(part.GetLocationPoint());
          }
        }
      }
      return points;
    }
  }
}
