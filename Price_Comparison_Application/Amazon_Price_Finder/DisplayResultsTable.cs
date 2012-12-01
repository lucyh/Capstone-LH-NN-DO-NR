﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;

namespace Price_Comparison
{
    //! The class for displaying the results table.
    /**
     * This class contains methods that display the records on the form.
     */
    class DisplayResultsTable
    {
        //! This is the method that updates the database (using the hardcoded set of records).
        /**
         * This method is passed the index of the record on the form that has 
         * been updated.  First it strips out all characters except digits and 
         * the decimal point.  Next it finds the record in the set and modifies
         * its database price and the difference between the online price and 
         * the database price.  Finally, it displays the table.
         */
        public static void UpdateDatabase(
          int index /*!< the index into the results table on the form for the record to be updated */
        )
        {
            PriceComparison.form.tblResults["DatabasePrice", index].Value 
                = Regex.Replace(PriceComparison.form.tblResults["DatabasePrice", index].Value.ToString(), @"[^\d\.-]", "");
            DataRow row = PriceComparison.set.Select("Barcode = '" + PriceComparison.form.tblResults["Barcode", index].Value.ToString() + "'")[0];
            double newPrice;
            if ( double.TryParse(PriceComparison.form.tblResults["DatabasePrice", index].Value.ToString(), out newPrice))
            {
                if ( newPrice > 0)
                {
                    row["dbPrice"] = PriceComparison.form.tblResults["DatabasePrice", index].Value;
                    row["diff"] = (Double)row["onlinePrice"] - (Double)row["dbPrice"];
                }
            }
            displayTable();
        }

        //! This is the method that displays the table on the form.
        /**
         * This method sorts the rows based on the current sort and filter 
         * parameters.  Then it sets the total records to display to the length
         * of the sorted array.  Then it calculates the first and last record 
         * on the current page.  It clears the previous results table, then 
         * iterates through the table starting with the first record through
         * the last record, adding a new row and displaying it for each record.
         * 
         * If there are no rows in the array, generally indicating that there
         * are no results in the current filter, a message is displayed 
         * indicating the set is empty.
         */
        public static void displayTable()
        {
            PriceComparison.sortedRows = sortFilterRows();
            PriceComparison.totalRecordsToDisplay = PriceComparison.sortedRows.Length;
            setFirstLastRecord();
            int count = 0;
            PriceComparison.form.tblResults.Rows.Clear();
            if (PriceComparison.sortedRows.Length > 0)
            {
                for (int i = PriceComparison.firstRecord; i <= PriceComparison.lastRecord && i < PriceComparison.totalRecordsToDisplay; i++)
                {
                    PriceComparison.form.tblResults.Rows.Add();
                    displayRow(count, PriceComparison.sortedRows[i]);
                    count++;
                }
            }
            else
            {
                PriceComparison.form.tblResults.Rows.Add();
                PriceComparison.form.tblResults["Description", 0].Value = "The current filter contains no records.";
                PriceComparison.form.lblDisplayedRecords.Text = "0-0 of 0";
            }
        }

        //! This is the method that displays a row on the form.
        /**
         * This method takes two parameters: the row number into the table on
         * the form and the row data itself.  First the barcode and description
         * are copied directly from the row data to the table.  The database
         * price and online price are formatted as "$x.xx".  The online price, 
         * however, is tested for whether it is equal to 0, and if it is "N/A"
         * is displayed instead of "$0.00" and the difference is displayed as 
         * "-" and set to 0.  If the difference is negative, it is displayed 
         * as "-x.xx", and the cell is colored red.  If the difference is 
         * positive, it is displayed as "+x.xx", and the cell is colored green.
         */
        public static void displayRow(
          int rowNum /*!< the row number of the record to be displayed */
        , DataRow row /*!< the data for the row to be displayed */
        )
        {
            PriceComparison.form.tblResults["Barcode", rowNum].Value = row["Barcode"].ToString();
            PriceComparison.form.tblResults["Description", rowNum].Value = row["Dscr"].ToString();
            PriceComparison.form.tblResults["DatabasePrice", rowNum].Value = String.Format("{0:$0.00}", (Double)row["dbPrice"]);

            if ((Double)row["onlinePrice"] == 0)
            {
                PriceComparison.form.tblResults["OnlinePrice", rowNum].Value = "N/A";
                PriceComparison.form.tblResults["Difference", rowNum].Value = "-";
                row["diff"] = 0;
            }
            else
            {
                PriceComparison.form.tblResults["OnlinePrice", rowNum].Value = String.Format("{0:$0.00}", (Double)row["onlinePrice"]);
                if ((Double)row["diff"] < 0)
                {
                    PriceComparison.form.tblResults["Difference", rowNum].Style.BackColor = System.Drawing.Color.Tomato;
                    PriceComparison.form.tblResults["Difference", rowNum].Value = String.Format("{0:0.00}", (Double)row["diff"]);
                }
                if ((Double)row["diff"] > 0)
                {
                    PriceComparison.form.tblResults["Difference", rowNum].Style.BackColor = System.Drawing.Color.GreenYellow;
                    PriceComparison.form.tblResults["Difference", rowNum].Value = String.Format("+{0:0.00}", (Double)row["diff"]);
                }
                if ((Double)row["diff"] == 0)
                {
                    PriceComparison.form.tblResults["Difference", rowNum].Style.BackColor = System.Drawing.Color.White;
                    PriceComparison.form.tblResults["Difference", rowNum].Value = "0.00";
                }
            }            
        }

        //! This is the method that sorts and filters the set.
        /**
         * This method selects and returns all rows that fit into the currently
         * assigned filter sorted by the currently assigned sort parameters.
         */
        public static DataRow[] sortFilterRows()
        {
            return PriceComparison.set.Select(PriceComparison.filter, PriceComparison.sortCol);
        }

        //! This is the method that sets the first and last record.
        /**
         * This method sets the number of the first and last records on the 
         * current page.  The first record is the current page times the 
         * number of results per page; the last record is the first record plus
         * the number of results per page minus 1.  If the number of the last 
         * record is higher than the total number of records, the last record
         * is set to the total number of records minus 1 (this is done because
         * the values act as indices into the sorted array).  
         * 
         * Next the total number of pages is set to the ceiling of total 
         * records divided by the number of results per page.  
         * 
         * Finally, text is displayed on screen indicating the records 
         * currently displayed.  It displays the first record plus 1 and the 
         * last record plus 1 because they are actually stored starting at 0 
         * instead of 1.
         */
        public static void setFirstLastRecord()
        {
            PriceComparison.firstRecord 
                = PriceComparison.currPage * PriceComparison.numResultsPerPage;
            PriceComparison.lastRecord 
                = PriceComparison.firstRecord + PriceComparison.numResultsPerPage - 1;
            if (PriceComparison.lastRecord >= PriceComparison.totalRecordsToDisplay)
            {
                PriceComparison.lastRecord = PriceComparison.totalRecordsToDisplay - 1;
            }
            PriceComparison.totalPages 
                = (int)(Math.Ceiling((Double)(PriceComparison.totalRecordsToDisplay / PriceComparison.numResultsPerPage)));
            PriceComparison.form.lblDisplayedRecords.Text 
                = (PriceComparison.firstRecord + 1).ToString() + "-" + (PriceComparison.lastRecord + 1).ToString() + " of " + PriceComparison.totalRecordsToDisplay.ToString();
        }

        //! This is the method that creates the hardcoded set.
        /**
         * This method contains the hardcoded data for the set.
         */
        public static void createSet()
        {
            PriceComparison.set.Rows.Add("008888526841", "Assassins Creed Revelations", 28, 0, 0);
            PriceComparison.set.Rows.Add("047875842069", "Call Of Duty MW3", 8.61, 0, 0);
            PriceComparison.set.Rows.Add("085391117018", "V for Vendetta", 8.25, 0, 0);
            PriceComparison.set.Rows.Add("024543563969", "Office Space", 8.61, 0, 0);
            PriceComparison.set.Rows.Add("22512000019", "MANGA Rurouni Kenshin Volume 6 BIG Edition", 17.99, 0, 0);
            PriceComparison.set.Rows.Add("22512000026", "DVD My Little Pony Friendship Is Magic", 14.99, 0, 0);
            PriceComparison.set.Rows.Add("20712000037", "DVD Emma Victorian Romance Season Two", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("20712000020", "DVD E'S Otherwise Complete", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("20712000013", "DVD Excel Saga Complete", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("20412000054", "MANGA The Sigh of Haruhi Suzumiya", 8.99, 0, 0);
            PriceComparison.set.Rows.Add("20412000047", "MANGA Bamboo Blade 10", 11.99, 0, 0);
            PriceComparison.set.Rows.Add("20412000030", "MANGA Bamboo Blade 6", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("20412000023", "MANGA Sayonara Zetsubo Sensei 3", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("20412000016", "DVD Virus Complete Collection", 39.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000010", "MANGA Hyde and Closer 6", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000072", "DVD Kenichi The Mighiest Disciple Season 1", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000034", "DVD Gunslinger Girl Il Teatrino", 59.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000065", "MANGA Spiral Volume 3", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000058", "MANGA Skip Beat 16", 8.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000041", "MANGA Skip Beat 25", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("4000501", "DVD L/R Volume 2", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("4000556", "DVD Nadia Nemo's Fortress", 5.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000096", "DVD Sekirei Season 1", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("4000510", "DVD Tenchi in Tokyo A New Enemy", 7.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000119", "MANGA Train Man A Shojo Manga", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000140", "MANGA Oresama Teacher 4", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000157", "MANGA St Dragon Girl 8", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000164", "MANGA Sgt Frog 15", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000188", "MANGA Mao-Chan Volume 2", 14.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000171", "MANGA St Dragon Girl 4", 8.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000201", "DVD One Piece Season 1 Part 3", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000195", "DVD One Piece Season 2 Part 1", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000218", "MANGA Slam Dunk 18", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000225", "MANGA YuGiOh! R 4", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("4000422", "DVD Tenchi in Tokyo A New Love", 7.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000270", "MANGA Arisa 4", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000263", "MANGA Slam Dunk 5", 7.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000256", "MANGA Slam Dunk 6", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000249", "MANGA Slam Dunk 12", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000232", "MANGA Slam Dunk 17", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000317", "DVD Full Metal Panic Fumoffu", 39.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000300", "DVD Strike Witches Season 1", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000294", "DVD Ikki Tousen Season 1", 19.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000324", "MANGA Oh My Goddess First End", 14.99, 0, 0);
            PriceComparison.set.Rows.Add("32312000331", "MANGA Pokemon Adventures Diamond 3", 7.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000026", "DVD Xenosaga SAVE Edition", 19.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000101", "DVD Pani Poni Dash SAVE Edition", 29.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000095", "DVD Shuffle SAVE Edition", 29.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000071", "DVD Guyver SAVE Edition", 29.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000064", "DVD Speed Grapher SAVE Edition", 29.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000057", "DVD Initial D 2nd/3rd Stage", 29.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000040", "DVD Initial D Fourth Stage", 29.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000033", "DVD Initial D First Stage", 29.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000118", "MERCH Death Note Misa Body Pillow", 44.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000125", "MERCH InuYasha Kikyo Keychain", 4.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000156", "MERCH Naruto Shippuden Anti Stone Headband", 21.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000149", "MERCH Hetalia Germany and Italy Notebook", 10.00, 0, 0);
            PriceComparison.set.Rows.Add("32412000132", "MERCH Code Geass Symbol Headband", 11.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000163", "MERCH Gravitation Suitcase PVC Keychain", 4.99, 0, 0);
            PriceComparison.set.Rows.Add("699858938971", "MERCH Trinity Blood Rosen Creuz Order Emblem", 7.00, 0, 0);
            PriceComparison.set.Rows.Add("6000034", "MERCH Trinity Blood Esther Metal Keychain", 7.00, 0, 0);
            PriceComparison.set.Rows.Add("6000022", "MERCH Trinity Blood AX Iron Maiden Icon Keychain", 7.00, 0, 0);
            PriceComparison.set.Rows.Add("6000129", "DVD I Shall Never Return", 19.99, 0, 0);
            PriceComparison.set.Rows.Add("6000110", "DVD Megazone 23", 39.99, 0, 0);
            PriceComparison.set.Rows.Add("6000101", "DVD Memories", 19.99, 0, 0);
            PriceComparison.set.Rows.Add("6000066", "DVD Cowboy Bebop Remix", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("6000059", "DVD Gurren Lagann Complete", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("6000044", "DVD Witch Hunter Robin Complete", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("6000097", "DVD Petshop of Horrors", 24.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000217", "MANGA Bunny Drop Volume 5", 12.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000200", "MANGA 13th Boy Volume 11", 11.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000194", "MANGA Bamboo Blade Volume 12", 11.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000187", "MANGA Bride's Story Volume 3", 16.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000224", "MERCH Cheetah Ame-Comi Figure", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000286", "MANGA Sailor V Volume 1", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000279", "MANGA Sailor V Volume 2", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000262", "MANGA Sailor Moon 1", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000255", "MANGA Sailor Moon Volume 2", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000248", "MANGA Sailor Moon Volume 3", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("32412000231", "MANGA Sailor Moon Volume 4", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000010", "DVD Linebarrels of Iron Part 2", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000027", "DVD Shura no Toki", 59.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000157", "MANGA Ultimo 6", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000164", "MERCH Gurren Lagann Keychain", 6.50, 0, 0);
            PriceComparison.set.Rows.Add("33012000171", "MERCH Ame-Comi Zatanna", 49.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000195", "FOOD Pocky Value Pack Chocolate", 10.00, 0, 0);
            PriceComparison.set.Rows.Add("33012000201", "FOOD Pocky Strawberry Large", 4.00, 0, 0);
            PriceComparison.set.Rows.Add("33012000140", "MANGA Ninja Girls 3", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000133", "MANGA Oh My Goddess Volume 20", 11.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000126", "MANGA Ninja Girls 9", 10.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000119", "MANGA Gantz 21", 12.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000102", "MANGA Ouran Host Club 4", 8.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000096", "MANGA Ouran Host Club 3", 8.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000089", "MANGA Ouran Host Club 2", 8.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000072", "MANGA Ouran Host Club 1", 8.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000065", "MANGA Ouran Host Club 13", 9.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000058", "MANGA Amazing Agent Luna Omnibus 6-7", 14.99, 0, 0);
            PriceComparison.set.Rows.Add("33012000041", "MANGA Gunslinger Girl Omnibus 9-10", 16.99, 0, 0);
            PriceComparison.set.Rows.Add("70462098617", "FOOD Sour Patch Kids 5 oz", 1.70, 0, 0);
            PriceComparison.set.Rows.Add("70462098679", "FOOD Sweedish Red Fish", 1.70, 0, 0);
            PriceComparison.set.Rows.Add("41420744020", "FOOD Black Forest Gummy Worms", 1.50, 0, 0);
            PriceComparison.set.Rows.Add("20709012319", "FOOD Trolli Sour Brite Octopus", 1.70, 0, 0);
            PriceComparison.set.Rows.Add("20709110435", "FOOD Trolli Soda Poppers", 1.70, 0, 0);
            PriceComparison.set.Rows.Add("41420744112", "FOOD Black Forest Gummy Cherries", 1.50, 0, 0);
            PriceComparison.set.Rows.Add("20709002402", "FOOD Trolli Classic Bears", 1.70, 0, 0);
        }
    }
}
