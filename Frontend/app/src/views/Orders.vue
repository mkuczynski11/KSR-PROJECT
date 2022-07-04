<template>
  <b-container>
    <b-row>
      <b-col cols="5" />
      <b-col cols="2"> </b-col>
      <b-col cols="5" />
    </b-row>
    <b-row v-if="ordersLoaded" class="py-4">
      <b-col cols="3"></b-col>
      <b-col>
        <b-table
          cols="6"
          v-if="ordersLoaded"
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
              @click="openOrderConfirmationForm(data.item)"
              >Potwierdź</b-button
            >
          </template>
          <template #empty="">
            <h4><b>No orders found</b></h4>
          </template>
        </b-table>
      </b-col>
      <b-col cols="3"></b-col>
    </b-row>
    <b-row v-else>
      <b-col cols="12" class="py-4">
        <b-spinner style="width: 3rem; height: 3rem" />
      </b-col>
    </b-row>
    <b-modal id="confirm-order-modal" size="lg" :hide-header="true">
      <b-container>
        <h4>Confirm Your order</h4>
      </b-container>
      <b-row class="pt-2">
        <b-col cols="6">
          <b>Price when ordered</b>
        </b-col>
        <b-col cols="6">
          <b>Current price</b>
        </b-col>
      </b-row>
      <b-row>
        <b-col cols="6" v-if="orderToConfirm">
          {{ orderToConfirm.unitPrice }}
        </b-col>
        <b-col cols="6" v-if="currentPriceLoaded">
          {{ currentPrice }}
        </b-col>
      </b-row>
      <b-row class="pt-2">
        <b-col cols="6">
          <b>Discount when ordered</b>
        </b-col>
        <b-col cols="6">
          <b>Current discount</b>
        </b-col>
      </b-row>
      <b-row>
        <b-col cols="6" v-if="orderToConfirm">
          {{ orderToConfirm.discount }}
        </b-col>
        <b-col cols="6" v-if="currentDiscountLoaded">
          {{ currentDiscount }}
        </b-col>
      </b-row>
      <template #modal-footer="{}">
        <b-button variant="danger" @click="closeOrderConfirmationForm()">
          Cancel
        </b-button>
        <b-button
          variant="success"
          @click="confirmOrder()"
          :disabled="!validForm()"
          >Confirm order</b-button
        >
      </template>
    </b-modal>
  </b-container>
</template>

<script>
import {getOrderStatus, getBooksPrices, getBooksDiscounts, confirmOrder} from "@/api/api.js";

const ORDER_FIELDS = ["ID", "bookName", "quantity", "totalPrice", "status", "confirm"];

export default {
  name: "Orders",
  data: function () {
    return {
      orders: [],
      items: [],
      fields: ORDER_FIELDS,
      ordersLoaded: false,
      form: {
        quantity: null,
      },
      orderToConfirm: null,
      currentPrice: null,
      currentPriceLoaded: false,
      currentDiscountLoaded: false,
    };
  },

  mounted() {
    this.parseOrders();
  },

  methods: {
    async parseOrders() {
      this.ordersLoaded = false;
      this.orders = this.$orders;
      this.items = [];

      for (let order of this.orders) {
        let statusReq = await getOrderStatus(order.orderId);
        let parsed_order = {
          id: order.orderId,
          bookName: order.book.name,
          bookId: order.book.id,
          quantity: order.quantity,
          totalPrice: order.quantity * order.book.unitPrice,
          unitPrice: order.book.unitPrice,
          discount: order.book.discount,
          status: statusReq.status
        };
        this.items.push(JSON.parse(JSON.stringify(parsed_order)));
      }

      this.ordersLoaded = true;
      this.watchForStatusChange();
    },

    watchForStatusChange() {
      setInterval(() => {
        for (let order of this.orders) {
          getOrderStatus(order.orderId).then(status => {
            let index = this.orders.indexOf(order);
            this.items[index].status = status.status;
          });
        }
      }, 3000);
    },

    async openOrderConfirmationForm(order) {
      this.orderToConfirm = order;
      await this.loadCurrentPrice(order.bookId);
      await this.loadCurrentDiscount(order.bookId);

      this.$bvModal.show("confirm-order-modal");
    },

    closeOrderConfirmationForm() {
      this.$bvModal.hide("confirm-order-modal");
    },

    validForm() {
      // TODO: Add conditions
      return true;
    },

    async loadCurrentPrice(bookId) {
      this.currentPriceLoaded = false;

      let prices = await getBooksPrices();
      this.currentPrice = prices.find(price => price.id === bookId).price;
      this.currentPriceLoaded = true;
    },

    async loadCurrentDiscount(bookId) {
      this.currentDiscountLoaded = false;

      let prices = await getBooksDiscounts();
      this.currentDiscount = prices.find(d => d.id === bookId).discount;
      this.currentDiscountLoaded = true;
    },

    async confirmOrder() {
      // TODO: Add logic about confirming order

      let res = await confirmOrder(this.orderToConfirm);

      if (res.status === 200) {
        this.$confirmedOrders.push(this.orderToConfirm);
        this.$bvToast.toast(`Successfully confirmed order!`, {
          title: "Order confirmation",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to confirm order!`, {
          title: "Order confirmation",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      this.closeOrderConfirmationForm();
    },

    // addToBasket(bookID) {
    //   this.basket.push({
    //     book: book,
    //     quantity: this.form.quantity,
    //   });
    //   this.form.quantity = null;
    //   this.$bvModal.hide("book-reservation-modal");
    // },

    // async viewUser(row) {
    //   await this.loadUser(row.item);
    //   row.toggleDetails();
    // },

    // async loadUser(userObject) {
    //   let { name, surname, email, birthdate } = await getUser(userObject.login);

    //   userObject.name = name;
    //   userObject.surname = surname;
    //   userObject.email = email;
    //   userObject.birthdate = birthdate;
    //   userObject.detailsLoaded = true;
    // },

    // async deleteUser(login) {
    //   await apiDeleteUser(login);
    //   this.parseUsers();
    // },

    // resetCreateForm() {
    //   for (let key in this.form) {
    //     this.form[key] = null;
    //   }
    // },

    // resetEditForm() {
    //   for (let key in this.userToEdit) {
    //     this.form[key] = null;
    //   }
    // },

    // async createUser() {
    //   console.log(this.form);
    //   await apiCreateUser(this.form);
    //   this.resetCreateForm();
    //   this.parseUsers();
    //   this.closeCreateUserForm();
    // },
  },
};
</script>

<style lang="scss" scoped></style>
