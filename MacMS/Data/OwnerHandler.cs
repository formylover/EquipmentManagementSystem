﻿using EquipmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EquipmentManagementSystem.Data {

    public class OwnerHandler : CommandHandler<ManagementContext> {


        public OwnerHandler(ManagementContext ctx) : base(ctx) { }


        public IQueryable<Owner> GetAll() {

            return context.Set<Owner>();
        }


        public IQueryable<Owner> GetAll(int page) {

            return context.Set<Owner>().Skip(page * PAGESIZE).Take(PAGESIZE);
        }


        public Owner Get(int id, bool include = true) {

            if (include) {
                return context.Set<Owner>().Where(o => o.ID == id).Include(o => o.Equipment).FirstOrDefault();
            }
            else {
                return context.Set<Owner>().Where(o => o.ID == id).FirstOrDefault();
            }
        }


        public IEnumerable<Owner> Sort(IQueryable<Owner> data, string sortOrder, int page) {

            var parameter = Expression.Parameter(typeof(Owner), "type");

            switch (sortOrder) {

                case "FirstName":
                case "FirstName_desc":

                    return sortOrder == "FirstName" ? GetSorted<Owner, string>(data, new List<string>() { "FirstName" }, page) : GetSorted<Owner, string>(data, new List<string>() { "FirstName" }, page, true);

                case "LastName":
                case "LastName_desc":

                    return sortOrder == "LastName" ? GetSorted<Owner, string>(data, new List<string>() { "LastName" }, page) : GetSorted<Owner, string>(data, new List<string>() {"LastName" }, page, true);

                case "Date":
                case "Date_desc":

                    return sortOrder == "Date" ? GetSorted<Owner, DateTime>(data, new List<string>() { "LastEdited" }, page) : GetSorted<Owner, DateTime>(data, new List<string>() { "LastEdited" }, page, true);

                default:
                    break;
            }

            return data;
        }


        public IQueryable<Owner> Search(string searchString) {

            var queryableData = GetAll();
            return GetData(ParseSearchString(searchString), queryableData);
        }


        public string[] ParseSearchString(string searchString) {

            var strArray = searchString.Split('+');

            for (int i = 0; i < strArray.Count(); i++) {

                strArray[i] = strArray[i].Replace(" ", "");
            }

            return strArray;
        }


        public IQueryable<Owner> GetData(string[] searchString, IQueryable<Owner> queryableData) {

            var queries = new List<List<Expression<Func<Owner, bool>>>>();

            for (int i = 0; i < searchString.Count(); i++) {

                queries.Add(new List<Expression<Func<Owner, bool>>>());
                var parameter = Expression.Parameter(typeof(Owner), "type");
                var constant = Expression.Constant(searchString[i], typeof(string));

                switch (searchString) {

                    // Checks if searchString matches standard datetime conventions i.e / and - separators
                    case var someVal when Regex.IsMatch(searchString[i], @"^\d[\d-\/\\]*"):

                        queries[i].AddRange(SearchDate(searchString[i], parameter));
                        break;

                    // Checks if searchString matches phone number pattern
                    case var someVal when Regex.IsMatch(searchString[i], @"^(\d{3,4})?\-?\ ?\d{5,7}$"):

                        queries[i].Add(SearchTelNr(searchString[i], parameter, constant));
                        break;
                    // Checks if Matces name
                    case var someVal when Regex.IsMatch(searchString[i], @"^\w+ ?\w+$"):

                        queries[i].AddRange(SearchName(searchString[i], parameter, constant));
                        break;

                    default:

                        throw new Exception("Invalid search criteria");
                }
            }

            return Search(queries, queryableData);
        }


        private List<Expression<Func<Owner, bool>>> SearchDate(string date, ParameterExpression parameter) {

            // Gets the LastEdited property
            var property = Expression.Property(parameter, "LastEdited");

            property = Expression.Property(property, "Date");

            // Gets the object.ToString() method
            var tostring = typeof(object).GetMethod("ToString");

            // Gets the string.Contains method
            var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            // e.LastEdited.Date.ToString()
            //var tostringValue = Expression.Call(property, tostring);

            date = date.Replace("/", " ");
            var dates = date.Split(' ');
            var queries = new List<Expression<Func<Owner, bool>>>();

            for (int i = 0; i < dates.Count(); i++) {

                var constant = Expression.Constant(dates[i], typeof(string));

                // (e => e.LastEdited.Date.ToString().Contains(constant) == searchString
                var exp = Contains(date, parameter, constant, property, true);

                queries.Add(Expression.Lambda<Func<Owner, bool>>(exp, parameter));
            }

            return queries;
        }


        private Expression<Func<Owner, bool>> SearchTelNr(string searchString, ParameterExpression parameter, ConstantExpression constant) {

            var property = Expression.Property(parameter, "TelNr");
            var exp = Contains(searchString, parameter, constant, property);

            return Expression.Lambda<Func<Owner, bool>>(exp, parameter);
        }


        private List<Expression<Func<Owner, bool>>> SearchName(string searchString, ParameterExpression parameter, ConstantExpression constant) {

            var queries = new List<Expression<Func<Owner, bool>>>();
            var property = Expression.Property(parameter, "FirstName");
            queries.Add(Contains(searchString, parameter, constant, property));

            property = Expression.Property(parameter, "LastName");
            queries.Add(Contains(searchString, parameter, constant, property));

            return queries;
        }


        private Expression<Func<Owner, bool>> Contains(string searchString, ParameterExpression parameter, ConstantExpression constant, MemberExpression property, bool toString = false) {

            var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            Expression exp;

            if (toString) {

                var toStringMethod = typeof(object).GetMethod("ToString");
                exp = Expression.Call(Expression.Call(property, toStringMethod), method, constant);
            }
            else {

                exp = Expression.Call(property, method, constant);
            }

            return Expression.Lambda<Func<Owner, bool>>((MethodCallExpression)exp, parameter);
        }


    }
}