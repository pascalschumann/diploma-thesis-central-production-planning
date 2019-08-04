﻿using Master40.Extensions;
using Master40.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Master40.DB.Data.Context;
using Master40.DB.Data.Helper;
using Master40.DB.DataModel;
using Microsoft.EntityFrameworkCore;

namespace Master40.ViewComponents
{
    public class ProductionTimelineViewComponent : ViewComponent
    {
        private readonly ProductionDomainContext _context;
        private readonly long _today;
        private int _orderId, _schedulingState;
        private GanttContext _ganttContext;
        public ProductionTimelineViewComponent(ProductionDomainContext context)
        {
            _context = context;
            _today = DateTime.Now.GetEpochMilliseconds();
            _ganttContext = new GanttContext();
        }

        /// <summary>
        /// called from ViewComponent.
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync(List<int> paramsList)
        {

            if (!_context.ProductionOrderOperations.Any())
            {
                return View("ProductionTimeline", _ganttContext);
            }

            //.Definitions();
            var orders = new List<int>();
            _orderId = paramsList[0];
            _schedulingState = paramsList[1];

                // If Order is not selected.
                if (_orderId == -1)
                {   // for all Orders
                    orders = _context?.CustomerOrderParts.Select(x => x.Id).ToList();
                }
                else
                {  // for the specified Order
                    orders = _context?.CustomerOrderParts.Where(x => x.CustomerOrderId == _orderId).Select(x => x.Id).ToList();
                }

            // Fill Select Fields
            var orderSelection = new SelectList(_context.CustomerOrders, "Id", "Name", _orderId);
            ViewData["OrderId"] = orderSelection.AddFirstItem(new SelectListItem { Value = "-1", Text = "All" });
            ViewData["SchedulingState"] = SchedulingState(_schedulingState);

            // return schedule
            return View("ProductionTimeline", _ganttContext);
        }
        

        /// <summary>
        /// Returns or creates corrosponding GanttTask Item with Property  type = "Project" and Returns it.
        /// -- Headline for one Project
        /// </summary>
        private GanttTask GetOrCreateTimeline(T_ProductionOrderOperation pow,int orderId)
        {
            IEnumerable<GanttTask> project;
            // get Timeline
            switch (_schedulingState)
            {
                case 3: // Machine Based
                    project = _ganttContext.Tasks
                        .Where(x => x.type == GanttType.project && x.id == "M" + (pow.MachineId ?? 0));
                    if (project.Any())
                    {
                        return project.First();
                    }
                    else
                    {
                        var gc = _ganttContext.Tasks.Count(x => x.type == GanttType.project) + 1;
                        var pt = CreateProjectTask("M" + pow.Machine.Id, pow.Machine.Name, "", 0, (GanttColors)gc);
                        _ganttContext.Tasks.Add(pt);
                        return pt;
                    }
                    //break;
                case 4: // Production Order Based
                    project = _ganttContext.Tasks
                        .Where(x => x.type == GanttType.project && x.id == "P" + pow.ProductionOrderId);
                    if (project.Any())
                    {
                        return project.First();
                    }
                    else
                    {
                        var gc = _ganttContext.Tasks.Count(x => x.type == GanttType.project) + 1;
                        var pt = CreateProjectTask("P" + pow.ProductionOrderId, "PO Nr.: " + pow.ProductionOrderId, "", 0, (GanttColors)gc);
                        _ganttContext.Tasks.Add(pt);
                        return pt;
                    }
                    //break;
                default: // back and forward
                    project = _ganttContext.Tasks
                        .Where(x => x.type == GanttType.project && x.id == "O" + orderId);
                    if (project.Any())
                    {
                        return project.First();
                    }
                    else
                    {
                        var gc = _ganttContext.Tasks.Count(x => x.type == GanttType.project) + 1;
                        var pt = CreateProjectTask("O" + orderId, _context.CustomerOrders.FirstOrDefault(x => x.Id == orderId).Name, "", 0, (GanttColors)gc);
                        _ganttContext.Tasks.Add(pt);
                        return pt;
                    }
                   // break;
            }
        }

        /// <summary>
        /// Defines start and end for the ganttchart based on the Scheduling State
        /// </summary>
        private void DefineStartEnd(ref long start, ref long end, T_ProductionOrderOperation item)
        {
            switch (_schedulingState)
            {
                case 1:
                    start = (_today + item.StartBackward.GetValueOrDefault() * 60000);
                    end = (_today + item.EndBackward.GetValueOrDefault() * 60000);
                    break;
                case 2:
                    start = (_today + item.StartForward.GetValueOrDefault() * 60000);
                    end = (_today + item.EndForward.GetValueOrDefault() * 60000);
                    break;
                default:
                    start = (_today + item.Start * 60000);
                    end = (_today + item.End * 60000);
                    break;
            }
        }

        /// <summary>
        /// Creates new TimelineItem with a label depending on the schedulingState
        /// </summary>
        public GanttTask CreateGanttTask(T_ProductionOrderOperation item, long start, long end, GanttColors gc, string parent)
        {
            var gantTask = new GanttTask()
            {
                id = item.Id.ToString(),
                type = GanttType.task,
                desc = item.Name,
                text = _schedulingState == 4 ? item.MachineGroup.Name : "P.O.: " + item.ProductionOrderId,
                start_date = start.GetDateFromMilliseconds().ToString("dd-MM-yyyy HH:mm"),
                end_date = end.GetDateFromMilliseconds().ToString("dd-MM-yyyy HH:mm"),
                IntFrom = start,
                IntTo = end,
                parent = parent,
                color = gc,
            };
            return gantTask;
        }

        private static GanttTask CreateProjectTask(string id, string name, string desc, int group, GanttColors gc)
        {
            return new GanttTask
            {
                id = id,
                text = name,
                desc = desc,
                type = GanttType.project,
                GroupId = group,
                color = gc
            };
        }

        /// <summary>
        /// Select List for Diagrammsettings (Forward / Backward / GT)
        /// </summary>
        private SelectList SchedulingState(int selectedItem)
        {
            var itemList = new List<SelectListItem> { new SelectListItem() { Text="Backward", Value="1"} };
            if (_context.ProductionOrderOperations.Any())
            {
                if (_context.ProductionOrderOperations.Max(x => x.StartForward) != 0)
                    itemList.Add(new SelectListItem() {Text = "Forward", Value = "2"});
            }
            itemList.Add(new SelectListItem() { Text = "Capacity-Planning Machinebased", Value = "3" });
            itemList.Add(new SelectListItem() { Text = "Capacity-Planning Productionorderbased", Value = "4" });
            return new SelectList( itemList, "Value", "Text", selectedItem);
        }
    }
}