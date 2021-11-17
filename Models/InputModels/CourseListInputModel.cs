using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MyCourse.Customization.ModelBinders;
using MyCourse.Models.Options;

namespace MyCourse.Models.InputModels
{
    [ModelBinder(BinderType = typeof(CourseListInputModelBinder))]
    public class CourseListInputModel
    {
        public CourseListInputModel(string search, int page, string orderBy, bool ascending,int limit, CoursesOrderOptions orderOptions)
        {

            //sanitizzazione
            if (!orderOptions.Allow.Contains(orderBy))
            {
                orderBy = orderOptions.By;
                ascending = orderOptions.Ascending;
            }


            Search = search;
            Page = Math.Max(1, page);
            Limit = Math.Max(1, limit);
            OrderBy = orderBy;
            Ascending = ascending;

            Offset = (Page - 1) * Limit;
        }

        public string Search { get; }
        public int Page { get; }
        public string OrderBy { get; }
        public bool Ascending { get; }

        public int Limit { get; }
        public int Offset { get; }
    }
}