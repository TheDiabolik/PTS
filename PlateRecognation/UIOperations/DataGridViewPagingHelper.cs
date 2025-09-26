using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class DataGridViewPagingHelper
    {
        // DataGridView'i güncelleyen method (Paging) 


        public static void LoadPagedData(int pageNumber, int pageSize, DataGridView dataGridView)
        {
            var pagedData = MainForm.m_mainForm.m_plateResults
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();




            DisplayManager.DataGridViewRowClearInvoke(dataGridView);

            foreach (var result in pagedData)
            {
                int newRowIndex = DisplayManager.DataGridViewAddRowPlateResultInvoke(dataGridView, result);


                //DisplayManager.DataGridViewFirstDisplayedScrollingRowIndexInvoke(m_dataGridViewPossiblePlateRegions.row, newRowIndex);
                //m_dataGridViewPossiblePlateRegions.Rows[newRowIndex].Selected = true;
            }

            if (dataGridView.Rows.Count > 0)
            {
                dataGridView.Rows[0].Selected = true;
            }
        }

        public static void AddLastItemToGrid(DataGridView dataGridView)
        {
            // Son elemanı DataGridView'e ekle
            //var lastItem = MainForm.m_mainForm.m_plateResults.Last();

            //threadsafe için deneme
            var lastItem = MainForm.m_mainForm.m_plateResults.ThreadSafeLast();

            int newRowIndex = DisplayManager.DataGridViewAddRowPlateResultInvoke(dataGridView, lastItem);

            // En son satırı seç
            int lastRowIndex = dataGridView.Rows.Count - 1;
            dataGridView.Rows[lastRowIndex].Selected = true;


            DisplayManager.DataGridViewFirstDisplayedScrollingRowIndexInvoke(dataGridView, lastRowIndex);
        }
    }
}
