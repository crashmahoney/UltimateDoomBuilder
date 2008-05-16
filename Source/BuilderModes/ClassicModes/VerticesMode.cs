
#region ================== Copyright (c) 2007 Pascal vd Heiden

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using CodeImp.DoomBuilder.Interface;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;

#endregion

namespace CodeImp.DoomBuilder.BuilderModes
{
	[EditMode(SwitchAction = "verticesmode",	// Action name used to switch to this mode
			  ButtonDesc = "Vertices Mode",		// Description on the button in toolbar/menu
		      ButtonImage = "VerticesMode.png",	// Image resource name for the button
			  ButtonOrder = int.MinValue + 0)]	// Position of the button (lower is more to the left)

	public class VerticesMode : ClassicMode
	{
		#region ================== Constants

		public const float VERTEX_HIGHLIGHT_RANGE = 20f;

		#endregion

		#region ================== Variables

		// Highlighted item
		protected Vertex highlighted;

		// Interface
		private bool editpressed;

		#endregion

		#region ================== Properties

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public VerticesMode()
		{
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Clean up

				// Dispose base
				base.Dispose();
			}
		}

		#endregion

		#region ================== Methods

		// Cancel mode
		public override void OnCancel()
		{
			base.OnCancel();
			
			// Return to this mode
			General.Map.ChangeMode(new VerticesMode());
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();
		}

		// Mode disengages
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Check which mode we are switching to
			if(General.Map.NewMode is LinedefsMode)
			{
				// Convert selection to linedefs

				// Clear selected vertices
				General.Map.Map.ClearSelectedVertices();
			}
			else if(General.Map.NewMode is SectorsMode)
			{
				// Convert selection to sectors
				
				// Clear selected vertices
				General.Map.Map.ClearSelectedVertices();
			}

			// Hide highlight info
			General.Interface.HideInfo();
		}

		// This redraws the display
		public override void OnRedrawDisplay()
		{
			// Render lines and vertices
			if(renderer.StartPlotter(true))
			{
				renderer.PlotLinedefSet(General.Map.Map.Linedefs);
				renderer.PlotVerticesSet(General.Map.Map.Vertices);
				if((highlighted != null) && !highlighted.IsDisposed)
					renderer.PlotVertex(highlighted, ColorCollection.HIGHLIGHT);
				renderer.Finish();
			}

			// Render things
			if(renderer.StartThings(true))
			{
				renderer.SetThingsRenderOrder(false);
				renderer.RenderThingSet(General.Map.Map.Things);
				renderer.Finish();
			}

			// Selecting?
			if(selecting)
			{
				// Render selection
				if(renderer.StartOverlay(true))
				{
					RenderMultiSelection();
					renderer.Finish();
				}
			}
			
			renderer.Present();
		}
		
		// This highlights a new item
		protected void Highlight(Vertex v)
		{
			// Update display
			if(renderer.StartPlotter(false))
			{
				// Undraw previous highlight
				if((highlighted != null) && !highlighted.IsDisposed)
					renderer.PlotVertex(highlighted, renderer.DetermineVertexColor(highlighted));

				// Set new highlight
				highlighted = v;

				// Render highlighted item
				if((highlighted != null) && !highlighted.IsDisposed)
					renderer.PlotVertex(highlighted, ColorCollection.HIGHLIGHT);
				
				// Done
				renderer.Finish();
				renderer.Present();
			}

			// Show highlight info
			if((highlighted != null) && !highlighted.IsDisposed)
				General.Interface.ShowVertexInfo(highlighted);
			else
				General.Interface.HideInfo();
		}
		
		// Selection
		protected override void OnSelect()
		{
			// Item highlighted?
			if((highlighted != null) && !highlighted.IsDisposed)
			{
				// Flip selection
				highlighted.Selected = !highlighted.Selected;

				// Redraw highlight to show selection
				if(renderer.StartPlotter(false))
				{
					renderer.PlotVertex(highlighted, renderer.DetermineVertexColor(highlighted));
					renderer.Finish();
					renderer.Present();
				}
			}
			else
			{
				// Start making a selection
				StartMultiSelection();
			}

			base.OnSelect();
		}
		
		// End selection
		protected override void OnEndSelect()
		{
			// Not stopping from multiselection?
			if(!selecting)
			{
				// Item highlighted?
				if((highlighted != null) && !highlighted.IsDisposed)
				{
					// Render highlighted item
					if(renderer.StartPlotter(false))
					{
						renderer.PlotVertex(highlighted, ColorCollection.HIGHLIGHT);
						renderer.Finish();
						renderer.Present();
					}
				}
			}

			base.OnEndSelect();
		}
		
		// Start editing
		protected override void OnEdit()
		{
			// Edit pressed in this mode
			editpressed = true;

			base.OnEdit();
		}
		
		// Done editing
		protected override void OnEndEdit()
		{
			editpressed = false;
			base.OnEndEdit();
		}
		
		// Mouse moves
		public override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// Not holding any buttons?
			if(e.Button == MouseButtons.None)
			{
				// Find the nearest vertex within highlight range
				Vertex v = General.Map.Map.NearestVertexSquareRange(mousemappos, VERTEX_HIGHLIGHT_RANGE / renderer.Scale);

				// Highlight if not the same
				if(v != highlighted) Highlight(v);
			}
		}

		// Mouse leaves
		public override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			
			// Highlight nothing
			Highlight(null);
		}

		// Mouse wants to drag
		protected override void OnDragStart(MouseEventArgs e)
		{
			base.OnDragStart(e);

			// Edit button used?
			if(General.Interface.CheckActionActive(null, "classicedit"))
			{
				// Anything highlighted?
				if((highlighted != null) && !highlighted.IsDisposed)
				{
					// Highlighted item not selected?
					if(!highlighted.Selected)
					{
						// Select only this vertex for dragging
						General.Map.Map.ClearSelectedVertices();
						highlighted.Selected = true;
					}

					// Start dragging the selection
					General.Map.ChangeMode(new DragVerticesMode(new VerticesMode(), highlighted, mousedownmappos));
				}
			}
		}

		// This is called wheh selection ends
		protected override void OnEndMultiSelection()
		{
			// Go for all vertices
			foreach(Vertex v in General.Map.Map.Vertices)
			{
				v.Selected = ((v.Position.x >= selectionrect.Left) &&
							  (v.Position.y >= selectionrect.Top) &&
							  (v.Position.x <= selectionrect.Right) &&
							  (v.Position.y <= selectionrect.Bottom));
			}

			base.OnEndMultiSelection();

			// Clear overlay
			if(renderer.StartOverlay(true)) renderer.Finish();

			// Redraw
			General.Interface.RedrawDisplay();
		}

		// This is called when the selection is updated
		protected override void OnUpdateMultiSelection()
		{
			base.OnUpdateMultiSelection();

			// Render selection
			if(renderer.StartOverlay(true))
			{
				RenderMultiSelection();
				renderer.Finish();
				renderer.Present();
			}
		}
		
		#endregion
	}
}
