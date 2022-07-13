<template>
  <b-container>
    <b-row v-if="invoicesLoaded" class="py-4">
      <b-col cols="1"></b-col>
      <b-col>
        <b-table
          cols="10"
          v-if="invoicesLoaded"
          :fields="fields"
          :items="items"
          show-empty
        >
          <template #cell(id)="data">
            {{ data.item.id }}
          </template>
          <template #cell(bookName)="data">
            {{ data.item.bookName }}
          </template>
          <template #cell(quantity)="data">
            {{ data.item.quantity }}
          </template>
          <template #cell(totalPrice)="data">
            {{ data.item.totalPrice }}
          </template>
          <template #cell(confirm)="data">
            <b-button
              variant="success"
              @click="openInvoiceConfirmationForm(data.item)"
              :disabled="
                data.item.status == 'Shipped to customer' ||
                data.item.status == 'Canceled'
              "
              >Potwierdź</b-button
            >
          </template>
          <template #empty="">
            <h4><b>No invoices found</b></h4>
          </template>
        </b-table>
      </b-col>
      <b-col cols="1"></b-col>
    </b-row>
    <b-row v-else>
      <b-col cols="12" class="py-4">
        <b-spinner style="width: 3rem; height: 3rem" />
      </b-col>
    </b-row>
    <b-modal id="pay-invoice-modal" size="md" :hide-header="true">
      <b-container>
        <h4>Confirm Your invoice</h4>
      </b-container>
      <template #modal-footer="{}">
        <b-button variant="danger" @click="closeInvoiceConfirmationForm()">
          Cancel
        </b-button>
        <b-button
          variant="success"
          @click="payInvoice()"
          >Pay invoice</b-button
        >
      </template>
    </b-modal>
  </b-container>
</template>

<script>
import {
  getOrderStatus,
  getBooksPrices,
  getBooksDiscounts,
  payInvoice,
} from "@/api/api.js";

const INVOICE_FIELDS = [
  "ID",
  "bookName",
  "quantity",
  "totalPrice",
  "status",
  "confirm",
];

export default {
  name: "Invoices",
  data: function () {
    return {
      invoices: [],
      items: [],
      fields: INVOICE_FIELDS,
      invoicesLoaded: false,
      invoiceToPay: null,
      currentPrice: null,
      currentPriceLoaded: false,
      currentDiscountLoaded: false,
    };
  },

  mounted() {
    this.parseInvoices();
  },

  methods: {
    async parseInvoices() {
      this.invoicesLoaded = false;
      this.invoices = this.$confirmedOrders;
      this.items = [];

      for (let order of this.invoices) {
        let statusReq = await getOrderStatus(order.id);
        let invoice = {
          id: order.id,
          bookName: order.bookName,
          bookId: order.bookId,
          quantity: order.quantity,
          totalPrice: order.quantity * order.unitPrice,
          unitPrice: order.unitPrice,
          discount: order.discount,
          status: statusReq.status,
        };
        this.items.push(JSON.parse(JSON.stringify(invoice)));
      }

      this.invoicesLoaded = true;
      this.watchForStatusChange();
    },

    async watchForStatusChange() {
      setInterval(() => {
        for (let invoice of this.invoices) {
          getOrderStatus(invoice.id).then((status) => {
            let index = this.invoices.indexOf(invoice);
            this.items[index].status = status.status;

            if (
              this.items[index].status == "Canceled" &&
              this.invoiceToPay.id == invoice.id
            ) {
              this.closeInvoiceConfirmationForm();
            }
          });
        }
      }, 3000);
    },

    async openInvoiceConfirmationForm(invoice) {
      this.invoiceToPay = invoice;
      await this.loadCurrentPrice(invoice.bookId);
      await this.loadCurrentDiscount(invoice.bookId);

      this.$bvModal.show("pay-invoice-modal");
    },

    closeInvoiceConfirmationForm() {
      this.$bvModal.hide("pay-invoice-modal");
    },

    async loadCurrentPrice(bookId) {
      this.currentPriceLoaded = false;

      let prices = await getBooksPrices();
      this.currentPrice = prices.find((price) => price.id === bookId).price;
      this.currentPriceLoaded = true;
    },

    async loadCurrentDiscount(bookId) {
      this.currentDiscountLoaded = false;

      let prices = await getBooksDiscounts();
      this.currentDiscount = prices.find((d) => d.id === bookId).discount;
      this.currentDiscountLoaded = true;
    },

    async payInvoice() {
      let res = await payInvoice(this.invoiceToPay);

      if (res.status === 200) {
        this.$paidOrders.push(this.invoiceToPay);
        getOrderStatus(this.invoiceToPay.id).then((status) => {
          let index = this.invoices.indexOf(this.invoiceToPay);
          this.items[index].status = status.status;
        });
        this.$bvToast.toast(`Invoice successfully paid!`, {
          title: "Inovoice payment",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to pay invoice!`, {
          title: "Invoice payment",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      this.closeInvoiceConfirmationForm();
    },
  },
};
</script>

<style lang="scss" scoped></style>
