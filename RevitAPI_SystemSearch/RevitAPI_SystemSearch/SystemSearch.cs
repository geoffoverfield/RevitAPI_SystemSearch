﻿
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
//Include other Namespaces based on your needs

namespace RevitAPI_SystemSearch
{
  public class SystemSearch
  {
    List<ElementId> m_lVistited, m_lSelectedElts;
    Document m_pDoc;
    Selection m_pSelectedElts;
    int m_iLoops;
    const int MAX_LOOPS = 20;

    public SystemSearch( UIApplication pUIApp )
    {
      InitializeLists();
      m_pDoc = pUIApp.ActiveUIDocument.Document;
      m_pSelectedElts = pUIApp.ActiveUIDocument.Selection;
      m_lSelectedElts = m_pSelectedElts.GetElementIds().ToList();

      RunStepThroughElements();
    }

    public void InitializeLists()
    {
      m_lSelectedElts = new List<ElementId>();
      m_lVistited = new List<ElementId>();
    }

    public void RunStepThroughElements()
    {
      Element pPrev = null, pNext = null;
      foreach( var pId in m_lSelectedElts )
      {
        if( m_lVistited.Contains( pId ) ) continue;
        if( pNext == null )
        {
          pPrev = m_pDoc.GetElement( pId );
          //Initial Element ^^^^^
          //Do what you want with the Element here... 
          //you won't be seeing it again.
          //Get data, etc.
          m_lVistited.Add( pId );
        }
        else pPrev = pNext;

        var pConnset = GetConnectors( pPrev );
        if( pConnset == null )
          continue;

        foreach( Connector pConn in pConnset )
        {
          pNext = GetNextConnectedElement(
            pConn, pPrev, ref m_lVistited );
          //Do what you want with the Elements here...
          //Get data, etc.
          //And make sure we mark it as visited 
          //so we don't come back to it.
          m_lVistited.Add( pNext.Id );
        }
        //This will run through every selected Element.  
        //If for some reason, we missed something, 
        //we make it recursive.
      }
      m_iLoops++;

      if( ( m_lVistited.Count < m_lSelectedElts.Count )
        && ( m_iLoops < MAX_LOOPS ) )
      {
        //We also don't want this infinitely spiraling, 
        //so we give it a max of 20 tries to look 
        //at everything.
        RunStepThroughElements();
      }
    }

    /// <summary>
    /// Return true if we want to look at the 
    /// next Connector, and false if the current 
    /// Connector is desired
    /// </summary>
    /// <param name="pConn">The Current Connector</param>
    /// <param name="pPrevElement">The previous Element the Connector came from</param>
    /// <param name="lVisited">A List of visited Elements (by their Id's)</param>
    /// <returns></returns>
    public bool NextConnector(
      Connector pConn,
      Element pPrevElement,
      List<ElementId> lVisited )
    {
      return ( pConn.Owner == pPrevElement )
        || ( pConn.Owner.Id == pPrevElement.Id )
        || ( pConn.Domain != Domain.DomainPiping ) //Change Domain based on need
        || ( lVisited.Contains( pConn.Owner.Id ) );
    }

    /// <summary>
    /// Gets the Element the Connector is connected 
    /// to if it has not been visited before. Currently 
    /// set to work with Pipe and FamilyInstance types
    /// - change types to accomodate
    /// </summary>
    /// <param name="pConn">The Connector from which we want to grab the connected Element</param>
    /// <param name="pPrevElem">The Element from which we are coming from</param>
    /// <param name="lVistied">List of visited Elements (by their Id's)</param>
    /// <returns></returns>
    public Element GetNextConnectedElement(
      Connector pConn,
      Element pPrevElem,
      ref List<ElementId> lVisited )
    {
      foreach( Connector pRef in pConn.AllRefs )
      {
        if( NextConnector( pRef, pPrevElem, lVisited ) )
        {
          continue;
        }
        return pRef.Owner;
      }
      return null;
    }

    public ConnectorSet GetConnectors( Element pElt )
    {
      if( pElt is Pipe )
        return ( (Pipe) pElt )?.ConnectorManager?
          .Connectors;
      if( pElt is FamilyInstance )
        return ( (FamilyInstance) pElt )?.MEPModel?
          .ConnectorManager?.Connectors;
      //Add other statements to accomodate your 
      //needs based on different element types
      return null;
    }
  }
}
